#!/bin/bash

# Colors for output
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Cleaning up Docker Swarm cluster...${NC}"

# Check if containers exist
if [ "$(docker ps -aq -f name=manager1)" ] || [ "$(docker ps -aq -f name=worker1)" ] || [ "$(docker ps -aq -f name=worker2)" ]; then
    
    # Try to remove any deployed stacks first
    echo -e "${YELLOW}Checking for deployed stacks...${NC}"
    STACKS=$(docker exec manager1 docker stack ls --format "{{.Name}}" 2>/dev/null)
    if [ -n "$STACKS" ]; then
        echo -e "${YELLOW}Removing stacks: $STACKS${NC}"
        for stack in $STACKS; do
            docker exec manager1 docker stack rm $stack
        done
        echo -e "${YELLOW}Waiting for stack services to shut down...${NC}"
        sleep 10
    fi
    
    # Remove messaging overlay network if it exists
    if [ "$(docker exec manager1 docker network ls -q -f name=^messaging$ 2>/dev/null)" ]; then
        echo -e "${YELLOW}Removing messaging overlay network...${NC}"
        docker exec manager1 docker network rm messaging 2>/dev/null
        echo -e "${GREEN}Messaging network removed${NC}"
    fi
    
    # Stop and remove containers
    echo -e "${YELLOW}Stopping and removing swarm nodes...${NC}"
    docker stop manager1 worker1 worker2 2>/dev/null
    docker rm -f manager1 worker1 worker2 2>/dev/null
    echo -e "${GREEN}Swarm nodes removed${NC}"
else
    echo -e "${YELLOW}No swarm nodes found${NC}"
fi

# Remove network
if [ "$(docker network ls -q -f name=swarm-net)" ]; then
    echo -e "${YELLOW}Removing swarm network...${NC}"
    docker network rm swarm-net 2>/dev/null
    echo -e "${GREEN}Network removed${NC}"
else
    echo -e "${YELLOW}No swarm network found${NC}"
fi

# Optional: Clean up tar files
if [ -f "messaging-producer.tar" ] || [ -f "messaging-consumer.tar" ]; then
    echo -e "${YELLOW}Found image tar files. Remove them? (y/n)${NC}"
    read -r response
    if [[ "$response" =~ ^[Yy]$ ]]; then
        rm -f messaging-producer.tar messaging-consumer.tar
        echo -e "${GREEN}Tar files removed${NC}"
    fi
fi

# Optional: Remove built images from local Docker
echo -e "${YELLOW}Remove local messaging images? (y/n)${NC}"
read -r response
if [[ "$response" =~ ^[Yy]$ ]]; then
    docker rmi messaging-producer:latest messaging-consumer:latest 2>/dev/null
    docker rmi localhost:5000/messaging-producer:latest localhost:5000/messaging-consumer:latest 2>/dev/null
    docker rmi registry:5000/messaging-producer:latest registry:5000/messaging-consumer:latest 2>/dev/null
    echo -e "${GREEN}Local images removed${NC}"
fi

# Optional: Remove registry
if [ "$(docker ps -aq -f name=registry)" ]; then
    echo -e "${YELLOW}Remove local registry container? (y/n)${NC}"
    read -r response
    if [[ "$response" =~ ^[Yy]$ ]]; then
        docker stop registry 2>/dev/null
        docker rm registry 2>/dev/null
        echo -e "${GREEN}Registry removed${NC}"
    fi
fi

echo -e "${GREEN}Cleanup complete!${NC}"
echo ""
echo "To start fresh, run: ./swarm-setup.sh"