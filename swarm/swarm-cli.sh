#!/bin/bash

# Helper script for common swarm operations
MANAGER="manager1"

case "$1" in
  shell)
    docker exec -it $MANAGER sh
    ;;
  nodes)
    docker exec $MANAGER docker node ls
    ;;
  services)
    docker exec $MANAGER docker service ls
    ;;
  ps)
    docker exec $MANAGER docker stack ps messaging
    ;;
  logs)
    if [ -z "$2" ]; then
      echo "Usage: $0 logs <service-name>"
      exit 1
    fi
    docker exec $MANAGER docker service logs "messaging_$2" -f
    ;;
  scale)
    if [ -z "$2" ] || [ -z "$3" ]; then
      echo "Usage: $0 scale <service-name> <replicas>"
      exit 1
    fi
    docker exec $MANAGER docker service scale "messaging_$2=$3"
    ;;
  deploy)
    docker cp docker-compose-swarm.yml $MANAGER:/docker-compose-swarm.yml
    docker exec $MANAGER docker stack deploy -c /docker-compose-swarm.yml messaging
    ;;
  remove)
    docker exec $MANAGER docker stack rm messaging
    ;;
  cleanup)
    docker rm -f manager1 worker1 worker2
    docker network rm swarm-net
    ;;
  *)
    echo "Usage: $0 {shell|nodes|services|ps|logs|scale|deploy|remove|cleanup}"
    exit 1
    ;;
esac


#chmod +x ./swarm-cli.sh
#./swarm-cli.sh
