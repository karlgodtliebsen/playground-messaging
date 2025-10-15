
#!/bin/bash
#cd /mnt/c/projects/docker/k3d/messaging
#chmod +x startup-k3d.sh

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}Starting k3d Messaging System...${NC}"
echo ""

# Step 1: Check if Docker is running
echo -e "${YELLOW}Step 1: Checking Docker...${NC}"
if ! docker ps > /dev/null 2>&1; then
    echo -e "${RED}Docker is not running. Please start Docker Desktop.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Docker is running${NC}"
echo ""

# Step 2: Check/Start Registry
echo -e "${YELLOW}Step 2: Checking Docker Registry...${NC}"
if [ ! "$(docker ps -q -f name=registry)" ]; then
    if [ "$(docker ps -aq -f name=registry)" ]; then
        echo "Starting existing registry container..."
        docker start registry
    else
        echo "Creating new registry container..."
        docker run -d -p 5000:5000 --name registry --restart=always registry:2
    fi
    sleep 3
fi
echo -e "${GREEN}✓ Registry is running on localhost:5000${NC}"
echo ""

# Step 3: Check if k3d cluster exists
echo -e "${YELLOW}Step 3: Checking k3d cluster...${NC}"
if ! k3d cluster list | grep -q "messaging"; then
    echo -e "${RED}k3d cluster 'messaging' not found!${NC}"
    echo "Creating new cluster..."
    
    # Create registry config
    cat > /tmp/k3d-registries.yaml <<'REGEOF'
mirrors:
  "localhost:5000":
    endpoint:
      - http://host.k3d.internal:5000
REGEOF
    
    # Create cluster
    k3d cluster create messaging \
      --registry-config /tmp/k3d-registries.yaml \
      --port "80:80@loadbalancer" \
      --port "443:443@loadbalancer"
    
    sleep 15
    
    echo -e "${GREEN}✓ Cluster created${NC}"
else
    echo "Cluster exists, checking if running..."
    
    # Start cluster if stopped
    if ! k3d cluster list | grep messaging | grep -q "running"; then
        echo "Starting k3d cluster..."
        k3d cluster start messaging
        sleep 10
    fi
    
    echo -e "${GREEN}✓ Cluster is running${NC}"
fi
echo ""

# Step 4: Verify cluster connectivity
echo -e "${YELLOW}Step 4: Verifying cluster connectivity...${NC}"
MAX_RETRIES=10
RETRY=0
while [ $RETRY -lt $MAX_RETRIES ]; do
    if kubectl cluster-info > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Cluster is accessible${NC}"
        break
    fi
    RETRY=$((RETRY+1))
    echo "Waiting for cluster... (attempt $RETRY/$MAX_RETRIES)"
    sleep 3
done

if [ $RETRY -eq $MAX_RETRIES ]; then
    echo -e "${RED}Failed to connect to cluster${NC}"
    exit 1
fi
echo ""

# Step 5: Check if nginx-ingress is installed
echo -e "${YELLOW}Step 5: Checking nginx ingress controller...${NC}"
if ! kubectl get namespace ingress-nginx > /dev/null 2>&1; then
    echo "Installing nginx ingress controller..."
    kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.1/deploy/static/provider/cloud/deploy.yaml
    
    echo "Waiting for ingress controller to be ready..."
    kubectl wait --namespace ingress-nginx \
      --for=condition=ready pod \
      --selector=app.kubernetes.io/component=controller \
      --timeout=120s
    
    echo -e "${GREEN}✓ Nginx ingress installed${NC}"
else
    echo -e "${GREEN}✓ Nginx ingress already installed${NC}"
fi
echo ""

# Step 6: Check if messaging namespace exists
echo -e "${YELLOW}Step 6: Checking messaging namespace...${NC}"
if ! kubectl get namespace messaging > /dev/null 2>&1; then
    echo "Creating messaging namespace..."
    kubectl create namespace messaging
fi
echo -e "${GREEN}✓ Namespace ready${NC}"
echo ""

# Step 7: Check if services are deployed
echo -e "${YELLOW}Step 7: Checking deployed services...${NC}"
PODS=$(kubectl get pods -n messaging 2>/dev/null | wc -l)
if [ "$PODS" -lt 2 ]; then
    echo -e "${YELLOW}No services deployed. Would you like to deploy now? (y/n)${NC}"
    read -r DEPLOY
    if [[ "$DEPLOY" =~ ^[Yy]$ ]]; then
        echo "Deploying services..."
        kubectl apply -f 01-namespace.yaml
        kubectl apply -f 02-kafka-minimal.yaml
        sleep 60
        kubectl apply -f 03-seq.yaml
        kubectl apply -f 04-redpanda-console.yaml
        kubectl apply -f 05-producer.yaml
        kubectl apply -f 06-consumer.yaml
        kubectl apply -f 07-ingress.yaml
        
        echo "Waiting for pods to start..."
        sleep 20
    fi
else
    echo -e "${GREEN}✓ Services already deployed${NC}"
fi
echo ""

# Step 8: Show status
echo -e "${BLUE}=== Current Status ===${NC}"
echo ""
kubectl get pods -n messaging
echo ""
kubectl get svc -n messaging
echo ""
kubectl get ingress -n messaging
echo ""

# Step 9: Show access URLs
echo -e "${BLUE}=== Access URLs ===${NC}"
echo -e "${GREEN}RedPanda Console:${NC} http://redpanda.local"
echo -e "${GREEN}Seq Logging:${NC}      http://seq.local"
echo ""
echo -e "${YELLOW}Note: Make sure these entries are in your hosts file:${NC}"
echo "  127.0.0.1    redpanda.local"
echo "  127.0.0.1    seq.local"
echo ""
echo -e "${BLUE}=== Useful Commands ===${NC}"
echo "  kubectl get pods -n messaging          # Check pod status"
echo "  kubectl logs -n messaging -l app=kafka -f  # View Kafka logs"
echo "  kubectl logs -n messaging -l app=producer -f  # View producer logs"
echo "  k3d cluster stop messaging             # Stop cluster"
echo "  k3d cluster start messaging            # Start cluster"
echo ""
echo -e "${GREEN}k3d Messaging System is ready!${NC}"


