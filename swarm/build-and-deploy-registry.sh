#!/bin/bash

SOLUTION_ROOT="/mnt/c/projects/playground-projects/playground-messaging/src"
PRODUCER_DOCKERFILE="Hosts/KafkaApplications/Messaging.Console.Kafka.Producer.App/Dockerfile"
CONSUMER_DOCKERFILE="Hosts/KafkaApplications/Messaging.Console.Kafka.Consumer.App/Dockerfile"
SWARM_DIR="/mnt/c/projects/playground-projects/playground-messaging/swarm"

echo "Building Docker images..."

cd $SOLUTION_ROOT

# Build and tag for registry using registry hostname
docker build -f $PRODUCER_DOCKERFILE -t registry:5000/messaging-producer:latest .
docker build -f $CONSUMER_DOCKERFILE -t registry:5000/messaging-consumer:latest .

echo "Starting local registry if not running..."

# Check if registry container exists and is on swarm-net
if [ ! "$(docker ps -q -f name=registry)" ]; then
    if [ "$(docker ps -aq -f name=registry)" ]; then
        echo "Starting existing registry..."
        docker start registry
    else
        echo "Creating new registry on swarm-net..."
        docker run -d -p 5000:5000 --name registry --hostname registry --network swarm-net registry:2
    fi
else
    echo "Registry already running"
fi

# Wait for registry to be ready
echo "Waiting for registry to be ready..."
sleep 3

# Tag for localhost (for pushing from host)
docker tag registry:5000/messaging-producer:latest localhost:5000/messaging-producer:latest
docker tag registry:5000/messaging-consumer:latest localhost:5000/messaging-consumer:latest

echo "Pushing images to local registry..."

docker push localhost:5000/messaging-producer:latest
docker push localhost:5000/messaging-consumer:latest

echo "Pulling images in swarm manager (using registry hostname)..."

docker exec manager1 docker pull registry:5000/messaging-producer:latest
docker exec manager1 docker pull registry:5000/messaging-consumer:latest

echo "Verifying images in manager..."
docker exec manager1 docker images | grep messaging

cd $SWARM_DIR

echo ""
echo "Images loaded successfully!"
echo "Ready to deploy! Use: ./swarm-cli.sh deploy"