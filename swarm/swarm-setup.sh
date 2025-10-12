#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}Creating Docker Swarm cluster...${NC}"

# Clean up any existing setup
docker rm -f manager1 worker1 worker2 2>/dev/null
docker network rm swarm-net 2>/dev/null

# Create network
echo -e "${GREEN}Creating network...${NC}"
docker network create --driver bridge swarm-net

# Start manager node with insecure registry
echo -e "${GREEN}Starting manager node...${NC}"
docker run -d --privileged --name manager1 \
  --hostname manager1 \
  --network swarm-net \
  -p 2377:2377 \
  -p 7946:7946 \
  -p 7946:7946/udp \
  -p 4789:4789/udp \
  -p 8090:8080 \
  -e DOCKER_TLS_CERTDIR="" \
  docker:dind \
  dockerd --insecure-registry=registry:5000

# Start worker nodes with insecure registry
echo -e "${GREEN}Starting worker nodes...${NC}"
docker run -d --privileged --name worker1 \
  --hostname worker1 \
  --network swarm-net \
  -e DOCKER_TLS_CERTDIR="" \
  docker:dind \
  dockerd --insecure-registry=registry:5000

docker run -d --privileged --name worker2 \
  --hostname worker2 \
  --network swarm-net \
  -e DOCKER_TLS_CERTDIR="" \
  docker:dind \
  dockerd --insecure-registry=registry:5000

# Wait for Docker daemon to start in containers
echo -e "${GREEN}Waiting for Docker daemons to start...${NC}"
sleep 10

# Initialize swarm
echo -e "${GREEN}Initializing swarm on manager...${NC}"
docker exec manager1 docker swarm init

# Get join token and manager IP
JOIN_TOKEN=$(docker exec manager1 docker swarm join-token worker -q)
MANAGER_IP=$(docker inspect -f '{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}' manager1)

# Join workers to swarm
echo -e "${GREEN}Joining workers to swarm...${NC}"
docker exec worker1 docker swarm join --token $JOIN_TOKEN $MANAGER_IP:2377
docker exec worker2 docker swarm join --token $JOIN_TOKEN $MANAGER_IP:2377

# Create messaging overlay network
echo -e "${GREEN}Creating messaging overlay network...${NC}"
docker exec manager1 docker network create --driver overlay --attachable messaging

# Verify cluster
echo -e "${BLUE}Cluster status:${NC}"
docker exec manager1 docker node ls

echo -e "${BLUE}Networks:${NC}"
docker exec manager1 docker network ls | grep -E "NAME|messaging"

echo -e "${GREEN}Swarm cluster ready!${NC}"
echo -e "${BLUE}Manager IP: $MANAGER_IP${NC}"
echo ""
echo "Usage examples:"
echo "  docker exec -it manager1 sh                    # Shell into manager"
echo "  docker exec manager1 docker node ls            # List nodes"
echo "  docker exec manager1 docker service ls         # List services"
echo "  docker exec manager1 docker stack deploy -c docker-compose.yml mystack"