#!/bin/bash


# Configuration
SOLUTION_ROOT="/mnt/c/projects/playground-projects/playground-messaging/src" 
PRODUCER_DOCKERFILE="Messaging.Console.Producer.App/Dockerfile"
SWARM_DIR="/mnt/c/projects/docker/swarm"

echo "Building Docker images..."

# Build images
cd $SOLUTION_ROOT
docker build -f $PRODUCER_DOCKERFILE -t messaging-producer:latest .

echo "Saving images to tar files..."

# Save to tar
docker save messaging-producer:latest -o $SWARM_DIR/messaging-producer.tar

echo "Loading images into Swarm manager..."

# Copy to manager
docker cp $SWARM_DIR/messaging-producer.tar manager1:/tmp/

# Load in manager
docker exec manager1 docker load -i /tmp/messaging-producer.tar

# Clean up tar files in container
docker exec manager1 rm /tmp/messaging-producer.tar 

echo "Images loaded successfully!"
docker exec manager1 docker images | grep messaging

cd $SWARM_DIR

echo ""
echo "Ready to deploy! Use: ./swarm-cli.sh deploy"


#chmod +x build-and-deploy-kafka-producer.sh