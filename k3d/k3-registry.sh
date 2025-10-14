# Create registries config directory
sudo mkdir -p /etc/rancher/k3s

# Create registries.yaml
sudo tee /etc/rancher/k3s/registries.yaml > /dev/null <<EOF
mirrors:
  "localhost:5000":
    endpoint:
      - "http://localhost:5000"
configs:
  "localhost:5000":
    tls:
      insecure_skip_verify: true
EOF

# Verify the file was created
cat /etc/rancher/k3s/registries.yaml

# Restart K3s to apply the config
sudo systemctl restart k3s

# Wait for K3s to come back up
sleep 15

# Verify K3s is running
kubectl get nodes