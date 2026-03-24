# TransitOps · Roadmap in Markdown

Source file: `TransitOps.Api/Docs/DailyRoadmap.pdf`

## Final Objective

Build and deliver a transport management backend with deployment on AWS, infrastructure as code, CI/CD, observability, basic security, and defensible technical documentation.

## Proposed Stack

- ASP.NET Core
- PostgreSQL
- Docker
- Terraform
- GitHub Actions
- AWS ECS Fargate
- Amazon ECR
- Amazon RDS
- Application Load Balancer
- CloudWatch
- Secrets Manager or SSM Parameter Store

## Execution Criterion

The project must remain small in functionality and deep in cloud operation. Each day must end with a verifiable result. If one day slips, the accessory detail moves, not the core of the roadmap.

## Week 1 · 24/03 to 29/03

### Phase 1 · Definition and Foundation

| Date | Focus | Specific Tasks | Expected Result |
| --- | --- | --- | --- |
| 24 Mar | Scope and stack definition | Finalize functional scope: transports, vehicles, drivers, events, and users. Lock the final stack: ASP.NET Core, PostgreSQL, Docker, Terraform, GitHub Actions, and AWS ECS Fargate. Create repository, base branches, and folder structure for `src`, `tests`, `infra`, and `docs`. | Initial README, fixed MVP backlog, and solution structure created. |
| 25 Mar | Domain modeling | Define the main entities and their relationships. Design transport states and transition rules. Write the initial database schema. | Initial entity-relationship diagram and list of business rules. |
| 26 Mar | Backend bootstrap | Create the Web API project and supporting layered projects. Configure dependency injection, `appsettings`, and environment profiles. Define route conventions, DTOs, and response structure. | The solution builds and the base API starts locally. |
| 27 Mar | Initial persistence | Integrate PostgreSQL and configure the connection string by environment. Create `DbContext`, entity configurations, and the first migration. Verify that the database is created correctly. | First migration applied and stable local connection. |
| 28 Mar | Transport CRUD | Implement `create`, `get by id`, `list`, `update`, and logical or physical `delete` for transports. Separate application and persistence layers. Manually test all endpoints. | Operational `Transport` CRUD and Postman collection or `.http` file. |
| 29 Mar | Vehicles and drivers | Implement entities, endpoints, and persistence for `Vehicle` and `Driver`. Add basic business validations. Review relationships and constraints. | Functional `Vehicle` and `Driver` CRUD and adjusted relational model. |

## Week 2 · 30/03 to 05/04

### Phase 2 · Logic and Quality

| Date | Focus | Specific Tasks | Expected Result |
| --- | --- | --- | --- |
| 30 Mar | Assignments | Implement the use case for assigning a vehicle and driver to a transport. Block invalid assignments according to state. Test complete creation and assignment flows. | Complete assignment use case and documented manual tests. |
| 31 Mar | Transport states | Implement the `planned -> in_transit -> delivered/cancelled` transition. Centralize transition validation. Add clear domain errors. | Basic state machine and documented rules. |
| 01 Apr | Logistics events | Create `ShipmentEvent` to record incidents and changes. Allow events to be associated with the transport. Design a queryable chronological history. | Event creation endpoint and history query. |
| 02 Apr | Listing and filtering | Implement filters by state, date range, and assignment. Add basic pagination and sorting. Optimize the response contract for listings. | Paginated listing and useful filters for the demo. |
| 03 Apr | Authentication | Implement JWT authentication. Define minimum roles: `admin` and `operator`. Protect sensitive endpoints. | Functional login and role-based authorization. |
| 04 Apr | Errors and API contract | Create global exception middleware. Normalize HTTP codes and error `payloads`. Document the most common error responses. | Homogeneous error handling and a more defensible API. |
| 05 Apr | Structured logging | Integrate structured logging with per-request context. Add `request id` or `correlation id`. Log critical operations without excessive noise. | Useful logs for operation and per-request traceability. |

## Week 3 · 06/04 to 12/04

### Phase 3 · Local Production

| Date | Focus | Specific Tasks | Expected Result |
| --- | --- | --- | --- |
| 06 Apr | Initial tests | Create unit tests for state rules and validations. Create integration tests for the main endpoints. Configure the test project and stable local execution. | Testing baseline ready and initial core coverage. |
| 07 Apr | Dockerfile | Create a multi-stage `Dockerfile` for the API. Reduce size and simplify runtime. Verify local image build. | Docker image built and reusable `Dockerfile`. |
| 08 Apr | Local `docker-compose` | Bring up API and PostgreSQL with `docker-compose`. Pass variables by environment. Ensure that any clone of the repository starts quickly. | Reproducible local environment and fast technical onboarding. |
| 09 Apr | Migrations and clean startup | Decide on a migration strategy at startup or as a separate script. Verify full recreation from scratch. Document the local bootstrap flow. | Project restartable without ambiguous manual steps. |
| 10 Apr | Health checks | Create `/health/live` and `/health/ready`. Check database connectivity in readiness. Prepare the app for deployment behind a load balancer. | Useful health checks for ECS and ALB. |
| 11 Apr | Query performance | Detect critical queries and improve `includes` and `joins`. Create initial indexes on search columns. Measure times before and after. | Main queries optimized and notes for the technical report. |
| 12 Apr | Input validations | Refine validations with FluentValidation or an equivalent solution. Ensure clear error messages. Avoid inconsistent states from input. | Robust validation and lower coupling in controllers. |

## Week 4 · 13/04 to 19/04

### Phase 4 · AWS + Terraform

| Date | Focus | Specific Tasks | Expected Result |
| --- | --- | --- | --- |
| 13 Apr | Refactor and cleanup | Remove duplication. Review names, responsibilities, and layer separation. Leave the backend ready before the jump to AWS. | Cleaner and more stable code. |
| 14 Apr | AWS and Terraform preparation | Configure AWS credentials and the Terraform project structure. Define modules or base folders per resource. Prepare variables, outputs, and an initial local backend. | Terraform initialized and baseline ready for IaC. |
| 15 Apr | Network: VPC and subnets | Create VPC, public subnets, and private subnets with Terraform. Design a minimal but correct scheme for the app and RDS. Tag resources consistently. | Network topology created. |
| 16 Apr | Network security | Define security groups for ALB, ECS, and RDS. Allow only strictly necessary traffic. Review public exposure and segmentation. | Functional network security baseline. |
| 17 Apr | Database on AWS | Create an RDS PostgreSQL instance. Configure storage, credentials, and private connectivity. Test the connection from a controlled environment. | RDS deployed and cloud persistence operational. |
| 18 Apr | Image registry | Create an ECR repository. Tag the image and push the first version manually. Validate authentication from local or CI. | Image flow to AWS working. |
| 19 Apr | ECS Cluster and Task Definition | Create the ECS cluster. Define the task definition with CPU, memory, variables, and logs. Prepare execution role and task role. | Application ready to run on Fargate. |

## Week 5 · 20/04 to 26/04

### Phase 5 · CI/CD

| Date | Focus | Specific Tasks | Expected Result |
| --- | --- | --- | --- |
| 20 Apr | ECS service and ALB | Create the Application Load Balancer and target group. Deploy ECS Service on Fargate. Verify external access to the API and health checks. | First accessible version on AWS. |
| 21 Apr | Basic CI | Create a GitHub Actions workflow for `restore`, `build`, and `test`. Fail the pipeline on build or test errors. Optionally publish badges. | Minimal CI operational. |
| 22 Apr | Docker build and push to ECR | Automate image build in GitHub Actions. Authenticate to AWS and push the image to ECR. Version image tags consistently. | Artifact pipeline completed. |
| 23 Apr | Terraform plan in CI | Run `terraform fmt`, `validate`, and `plan` from the pipeline. Separate sensitive variables. Save the plan or summary for review. | Automated infrastructure change control. |
| 24 Apr | Controlled Terraform apply | Define how changes are applied: manual approval or a specific branch. Avoid accidental deployments. Check idempotency. | Safer deployment process. |
| 25 Apr | Automatic ECS deployment | Update the task definition with the new image. Force a new deployment after merge. Confirm that the new version becomes active. | Functional CD. |
| 26 Apr | Post-deploy smoke tests | Add basic checks after deployment. Verify login, health, and a minimum business case. Fail the pipeline if the deployment is not usable. | More reliable end-to-end pipeline. |

## Week 6 · 27/04 to 03/05

### Phase 6 · Observability and Security

| Date | Focus | Specific Tasks | Expected Result |
| --- | --- | --- | --- |
| 27 Apr | CI/CD consolidation | Review complete pipelines and timings. Clean redundant steps. Document the integration and delivery flow. | CI/CD ready to present. |
| 28 Apr | Logs in CloudWatch | Send container logs to CloudWatch. Verify format, filters, and search. Ensure the logs are useful to diagnose real failures. | Basic observability in production. |
| 29 Apr | Operational metrics | Review ECS, ALB, and RDS metrics. Choose the ones that will appear in the demo and technical report. Note relevant indicators: CPU, memory, errors, and latency. | Minimum set of metrics defined. |
| 30 Apr | Dashboard | Create a dashboard in CloudWatch. Group infrastructure and application metrics. Prepare a clear visualization for the defense. | Operations dashboard available. |
| 01 May | Alarms | Create alarms for high CPU, errors, or unavailability. Define reasonable thresholds for an academic environment. Test that they trigger in a controlled scenario if possible. | Alerting baseline implemented. |
| 02 May | Secrets management | Move credentials and secrets to AWS Secrets Manager or SSM. Remove any secret from the code and the pipeline. Document the secure configuration strategy. | Secure configuration externalized. |
| 03 May | Least-privilege IAM | Review ECS roles, GitHub Actions roles, and Terraform access. Reduce unnecessary permissions. Document the principle of least privilege. | More defensible IAM. |

## Week 7 · 04/05 to 10/05

### Phase 7 · Technical Improvement

| Date | Focus | Specific Tasks | Expected Result |
| --- | --- | --- | --- |
| 04 May | General security review | Check exposed ports, sensitive logs, and public configuration. Review authentication and authorization. Close detected security debt. | Acceptable security baseline. |
| 05 May | OpenTelemetry or basic traces | Instrument the application with traces or equivalent telemetry. Propagate identifiers between logs and requests. Prepare a clear observability story for the technical report. | Telemetry above the minimum. |
| 06 May | Logging improvement | Add business context: `transport id`, state, and user. Avoid logging unnecessary data. Make a real failure easier to trace. | More useful logs for support. |
| 07 May | Rate limiting or basic protection | Apply request limiting or equivalent protection. Restrict simple abuse. Document why it is included as an operational measure. | API more robust against basic abuse. |
| 08 May | Resilience | Introduce controlled `retry` where it makes sense. Review timeouts and dependency handling. Avoid silent failures or unnecessary blocking. | More stable behavior under transient errors. |
| 09 May | Migration strategy | Define how the schema will evolve in future deployments. Separate migration from application startup if necessary. Write the technical justification. | Clear story of database changes. |
| 10 May | Backups and recovery | Review RDS snapshots or backups. Document approximate RPO and RTO at an academic level. Explain what is recovered and what is not. | Operational continuity section ready. |

## Week 8 · 11/05 to 17/05

### Phase 8 · Diagrams and Documentation

| Date | Focus | Specific Tasks | Expected Result |
| --- | --- | --- | --- |
| 11 May | Simple load testing | Run a light load with `k6`, `hey`, or a similar tool. Measure times and errors in a basic scenario. Extract 2 or 3 useful conclusions, not just numbers. | Measurable results for the technical report. |
| 12 May | Context diagram | Create C4 level 1 with actors and system. Represent the operating company, API, and relevant external services. Align the diagram with the real scope. | Context diagram ready. |
| 13 May | Container diagram | Create C4 level 2. Reflect API, database, CI/CD, and key AWS components. Avoid including elements that were not implemented. | Consistent container diagram. |
| 14 May | AWS deployment diagram | Represent VPC, subnets, ALB, ECS, RDS, and flows. Indicate what is in the public network and what is in the private network. Use the diagram to explain security and operation. | Clear deployment diagram. |
| 15 May | Pipeline diagram | Draw CI/CD from commit to deployment. Include build, test, image, ECR, Terraform, and ECS. Make the automation clear. | Defensible visual pipeline. |
| 16 May | Data model | Generate the final data model. Review names, keys, and indexes. Adjust the documentation to what was actually deployed. | Final relational model. |
| 17 May | Architecture writing | Write the backend architecture section and the reasons for the modular monolith. Explain layers and responsibilities. Justify why microservices were not used. | Architecture section almost complete. |

## Week 9 · 18/05 to 24/05

### Phase 9 · Technical Report

| Date | Focus | Specific Tasks | Expected Result |
| --- | --- | --- | --- |
| 18 May | Technical decisions | Write summarized ADRs or a decision table. Justify ECS over EKS, Terraform over console, and RDS over a local alternative. Note real trade-offs. | Decisions section ready. |
| 19 May | Introduction and objectives | Write the motivation for the TFG. Describe the problem, general objective, and specific objectives. Align the text with the transition to cloud and DevOps. | Opening of the technical report prepared. |
| 20 May | Functional requirements | Document the implemented use cases. Clearly separate what was done from what was discarded. Add summarized acceptance criteria. | Functional requirements closed. |
| 21 May | Non-functional requirements | Write availability, security, maintainability, deployment, and observability requirements. Relate them to the implemented decisions. Avoid requirements that cannot be demonstrated. | Defensible non-functional requirements. |
| 22 May | Backend section | Explain the structure of the API, layers, endpoints, and persistence. Add flow examples. Insert screenshots or small fragments if they add value. | Advanced backend chapter. |
| 23 May | Cloud section | Explain AWS, network, deployment, and basic operation. Include Terraform as the axis of reproducibility. Relate architecture to security. | Advanced cloud chapter. |
| 24 May | CI/CD and observability | Write the pipeline, deployment strategy, logging, metrics, and alarms. Include dashboard screenshots if appropriate. Highlight automation and operation. | DevOps chapter almost ready. |

## Week 10 · 25/05 to 31/05

### Phase 10 · Closure

| Date | Focus | Specific Tasks | Expected Result |
| --- | --- | --- | --- |
| 25 May | Security and testing | Write authentication, IAM, secrets, unit, integration, and load testing. Summarize results concretely. Avoid excessive promises. | Quality and security chapter completed. |
| 26 May | Results | Write exactly what was achieved. Compare the initial objective and the final scope. Add metrics or verifiable milestones. | Results section finished. |
| 27 May | Costs | Estimate the approximate monthly deployment cost. Identify which services have the greatest impact. Explain that the environment is academic and limited. | Cost analysis included. |
| 28 May | Limitations | Indicate what was not implemented: frontend, advanced scaling, multi-region, and similar items. Turn limitations into technical honesty. Avoid making them seem like conceptual failures. | Well-argued limitations. |
| 29 May | Future work | Propose realistic improvements: queues, events, more observability, autoscaling, or frontend. Relate them to professional growth toward Cloud Engineer or DevOps. Keep the focus on system continuity. | Coherent future work. |
| 30 May | Full review | Review the technical report, diagrams, spelling, links, and technical consistency. Check that the repository is clean and presentable. Run the full demo one last time. | Final candidate version. |
| 31 May | Final delivery | Prepare ZIP or repository, technical report, presentation, and demo script. Verify screenshots, commands, credentials, and defense materials. Close the final delivery checklist. | Project ready to deliver and defend. |

## Weekly Control Checklist

| # | Control | What to Validate |
| --- | --- | --- |
| 1 | Clean build | The solution builds without strange manual steps. |
| 2 | Runnable tests | Existing tests run locally and in CI. |
| 3 | Reproducible environment | Another team could start the project with clear instructions. |
| 4 | Versioned infrastructure | AWS changes go through Terraform. |
| 5 | Operational demo | The functionality implemented that week can be shown in 5 minutes. |
| 6 | Up-to-date documentation | README, diagrams, and notes are not pushed to the end. |

## Expected Result by May 31

Functional backend, deployed on AWS, infrastructure defined with Terraform, operational CI/CD pipeline, visible logs and metrics, technical report written, and defense material prepared.
