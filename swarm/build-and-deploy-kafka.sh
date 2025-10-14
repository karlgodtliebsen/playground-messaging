#!/bin/bash

# Configuration
SOLUTION_ROOT="/mnt/c/projects/playground-projects/playground-messaging/src" 
PRODUCER_DOCKERFILE="Hosts/KafkaApplications/Messaging.Console.Kafka.Producer.App/Dockerfile"
CONSUMER_DOCKERFILE="Hosts/KafkaApplications/Messaging.Console.Kafka.Consumer.App/Dockerfile"
SWARM_DIR="/mnt/c/projects/playground-projects/playground-messaging/swarm"

echo "Building Docker images..."

# Build images
cd $SOLUTION_ROOT
docker build -f $PRODUCER_DOCKERFILE -t messaging-producer:latest .
docker build -f $CONSUMER_DOCKERFILE -t messaging-consumer:latest .

echo "Saving images to tar files..."

# Save to tar
docker save messaging-producer:latest -o $SWARM_DIR/messaging-producer.tar
docker save messaging-consumer:latest -o $SWARM_DIR/messaging-consumer.tar

# Verify tar files were created
if [ ! -f "$SWARM_DIR/messaging-producer.tar" ]; then
    echo "ERROR: messaging-producer.tar was not created!"
    exit 1
fi

if [ ! -f "$SWARM_DIR/messaging-consumer.tar" ]; then
    echo "ERROR: messaging-consumer.tar was not created!"
    exit 1
fi

echo "Tar files created successfully"
ls -lh $SWARM_DIR/*.tar

echo "Loading images into Swarm manager..."

# Copy to manager
echo "Copying producer tar..."
docker cp $SWARM_DIR/messaging-producer.tar manager1:/tmp/
echo "Copying consumer tar..."
docker cp $SWARM_DIR/messaging-consumer.tar manager1:/tmp/

# Verify files are in container
echo "Verifying files in container..."
docker exec manager1 ls -lh /tmp/messaging-*.tar

# Load in manager
echo "Loading producer image..."
docker exec manager1 docker load -i /tmp/messaging-producer.tar

echo "Loading consumer image..."
docker exec manager1 docker load -i /tmp/messaging-consumer.tar

# Clean up tar files in container
echo "Cleaning up tar files in container..."
docker exec manager1 rm /tmp/messaging-producer.tar /tmp/messaging-consumer.tar

echo "Images loaded successfully!"
docker exec manager1 docker images | grep messaging

cd $SWARM_DIR

echo ""
echo "Ready to deploy! Use: ./swarm-cli.sh deploy"

#chmod +x build-and-deploy.sh