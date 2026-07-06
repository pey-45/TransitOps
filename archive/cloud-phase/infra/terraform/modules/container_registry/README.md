# Container Registry Module

Creates the ECR repository used to store TransitOps API container images.

Current scope:

- ECR repository using the `<project>/<service>` convention.
- Image scanning on push.
- AES256 repository encryption.
- Lifecycle policy that keeps a bounded number of images.
