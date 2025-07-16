# Docker Certificate Volume Configuration

## Overview
The PublicKeyServer now stores CA certificates in a dedicated `certificates` directory. This directory can be mounted as a Docker volume to persist certificates across container restarts.

## How It Works
The application automatically:
1. Creates a `certificates` directory if it doesn't exist
2. Stores all certificate files (`*.pem`) in this directory
3. Reads certificates from this directory when needed

## Certificate Storage
- Certificates are stored in the `./certificates` directory on the host
- Inside the container, certificates are stored in `/app/certificates`
- Certificate files follow the naming pattern: 
  - `cacert.{origin}.pem` - Root CA certificate
  - `subcacert.{origin}.pem` - Sub CA certificate
  - `subcakeys.{origin}.pem` - Sub CA private keys
  - `SAVE-ME-OFFLINE-cakeys.{origin}.pem` - Root CA private keys (keep this secure!)

## Docker Compose Configuration
The `docker-compose.yml` file includes a volume mount:
```yaml
volumes:
  - ./certificates:/app/certificates
```

## Usage
1. Create the certificates directory on your host (optional, will be created automatically):
   ```bash
   mkdir -p ./certificates
   chmod 700 ./certificates  # Secure the directory
   ```

2. Start the container:
   ```bash
   docker-compose up -d
   ```

3. Certificates will be automatically created in `./certificates` on first run
4. On subsequent runs, existing certificates will be preserved and reused

## Backup
To backup your certificates, simply copy the `./certificates` directory to a safe location:
```bash
cp -r ./certificates ./certificates-backup-$(date +%Y%m%d)
```

## Security Notes
- The `SAVE-ME-OFFLINE-cakeys.{origin}.pem` file contains the root CA private key and should be stored offline in a secure location after creation
- Ensure proper permissions on the `./certificates` directory:
  ```bash
  chmod 700 ./certificates
  chmod 600 ./certificates/*.pem
  ``` 