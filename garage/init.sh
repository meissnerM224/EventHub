set -e

if garage layout show | grep -q 'No nodes currently have a role'; then
  NODE_ID=$(garage status | awk '/NO ROLE ASSIGNED/ {print $1}')
  garage layout assign -z dc1 -c 10G "$NODE_ID"
  garage layout apply --version 1
fi

garage bucket create media 2>/dev/null || true
garage bucket website --allow media

if ! garage key list | grep -q "$STORAGE_ACCESS_KEY"; then
  garage key import --yes "$STORAGE_ACCESS_KEY" "$STORAGE_SECRET_KEY" -n eventhub-api
fi

garage bucket allow --read --write media --key "$STORAGE_ACCESS_KEY"

echo "Garage ready"