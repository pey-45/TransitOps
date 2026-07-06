# Sprint 2 Terraform Foundation Explanation

## 1. Que es Terraform y por que se usa aqui

Terraform es una herramienta de Infrastructure as Code. Eso significa que, en vez de crear infraestructura a mano en AWS con clicks, se describe en ficheros `.tf`: redes, subredes, reglas de seguridad, balanceadores, bases de datos, repositorios de imagenes y otros recursos cloud.

Terraform trabaja comparando dos cosas: lo que esta escrito en el codigo y lo que existe realmente en AWS. Con `terraform plan` muestra que cambios haria, sin tocar nada. Con `terraform apply` aplica esos cambios de verdad. Ese ciclo permite revisar antes de crear, modificar o destruir infraestructura.

En TransitOps se usa porque el objetivo del proyecto no es solo tener una API local, sino demostrar una base cloud reproducible y defendible. Terraform permite versionar la infraestructura en Git, reutilizar convenciones, separar entornos y preparar un despliegue serio en AWS sin depender de pasos manuales.

## 2. Estructura del repositorio Terraform

La infraestructura vive bajo `infra/terraform/`.

```text
infra/
`-- terraform/
    |-- bootstrap/
    |   `-- remote_state/
    |       |-- main.tf
    |       |-- variables.tf
    |       |-- locals.tf
    |       |-- outputs.tf
    |       |-- providers.tf
    |       |-- versions.tf
    |       `-- terraform.tfvars.example
    |-- modules/
    |   `-- platform_foundation/
    |       |-- main.tf
    |       |-- variables.tf
    |       |-- locals.tf
    |       `-- outputs.tf
    `-- environments/
        |-- dev/
        |   |-- main.tf
        |   |-- variables.tf
        |   |-- outputs.tf
        |   |-- providers.tf
        |   |-- versions.tf
        |   |-- backend.hcl.example
        |   `-- terraform.tfvars.example
        `-- prod/
            |-- main.tf
            |-- variables.tf
            |-- outputs.tf
            |-- providers.tf
            |-- versions.tf
            |-- backend.hcl.example
            `-- terraform.tfvars.example
```

`bootstrap/` contiene infraestructura necesaria para que Terraform funcione bien: el bucket S3 del estado remoto y la tabla DynamoDB de bloqueo. Es un paso previo y separado.

`modules/` contiene bloques reutilizables. En este sprint, `platform_foundation` define la base comun de red, nombres, tags, rutas y security groups.

`environments/` contiene los puntos de entrada reales por entorno. `dev` y `prod` invocan el mismo modulo, pero con valores concretos para cada entorno. En la practica, se trabajara primero con `dev`; `prod` queda preparado para el futuro.

## 3. Remote State: el problema y la solucion

Terraform guarda informacion sobre lo que gestiona en un fichero llamado state. Ese state contiene IDs reales de AWS, relaciones entre recursos y el ultimo estado conocido. Sin state, Terraform no sabria con seguridad que recursos creo ni como actualizarlos.

Si el state vive solo localmente en un `terraform.tfstate`, hay varios problemas: se puede perder, no es comodo para CI/CD, otra persona o pipeline no puede reutilizarlo facilmente, y dos ejecuciones simultaneas podrian pisarse. Por eso se usa remote state: el estado vive en un backend compartido.

En TransitOps, `bootstrap/remote_state/` crea ese backend compartido con S3 y DynamoDB:

- `aws_s3_bucket.terraform_state`: crea el bucket donde se guardaran los states.
- `aws_s3_bucket_versioning.terraform_state`: activa versionado para poder recuperar versiones anteriores del state si algo sale mal.
- `aws_s3_bucket_server_side_encryption_configuration.terraform_state`: cifra el state en reposo con `AES256`.
- `aws_s3_bucket_public_access_block.terraform_state`: bloquea acceso publico al bucket.
- `aws_s3_bucket_ownership_controls.terraform_state`: fuerza que la cuenta propietaria del bucket sea propietaria de los objetos.
- `aws_dynamodb_table.terraform_locks`: crea la tabla de locking. DynamoDB es una base NoSQL gestionada por AWS; aqui no se usa para negocio, sino para que Terraform ponga un candado mientras una ejecucion esta modificando el state.

El locking evita que dos `terraform apply` trabajen sobre el mismo state al mismo tiempo. Sin ese bloqueo, una ejecucion podria sobrescribir o corromper el trabajo de otra.

## 4. El modulo platform_foundation

Un modulo Terraform es una pieza reutilizable de infraestructura. Es parecido a una funcion: acepta parametros, calcula valores internos, crea recursos y expone resultados. `platform_foundation` es el modulo que define la base de red y seguridad que despues usaran ECS, RDS y ALB.

### variables.tf

El modulo acepta estos parametros principales:

- `project_slug`: identificador corto del proyecto, por ejemplo `transitops`.
- `service_slug`: servicio principal, por ejemplo `api`.
- `environment`: entorno logico, limitado a `dev` o `prod`.
- `aws_region`: region AWS usada por el entorno, por ejemplo `eu-west-1`.
- `owner`: valor del tag `Owner`.
- `repository`: valor del tag `Repository`.
- `root_domain`: dominio raiz opcional para calcular el hostname esperado de la API.
- `availability_zones`: lista de zonas de disponibilidad, por ejemplo `eu-west-1a` y `eu-west-1b`.
- `vpc_cidr`: rango principal de red de la VPC. Por defecto es `10.0.0.0/16`.
- `api_container_port`: puerto interno de la API en ECS. Por defecto es `8080`.
- `tags_override`: mapa opcional para anadir o sobrescribir tags.

Un CIDR es una forma compacta de definir un rango de IPs. Por ejemplo, `10.0.0.0/16` representa una red grande privada que luego se divide en subredes mas pequenas.

### locals.tf

`locals.tf` calcula valores internos para no repetir logica por todo el modulo:

- `name_prefix`: combina proyecto y entorno, por ejemplo `transitops-dev`.
- `common_tags`: tags obligatorios comunes como `Project`, `Environment`, `ManagedBy`, `Owner`, `Repository` y `Service`.
- `api_service_tags`: tags comunes pero marcando `Service = api`.
- `dns_api_hostname`: hostname esperado si existe dominio, por ejemplo `api.dev.example.com`.
- `secrets_prefix`: prefijo de Secrets Manager, por ejemplo `transitops/dev/app`.
- `parameters_prefix`: prefijo de SSM Parameter Store, por ejemplo `/transitops/dev/app`.
- `terraform_state_key`: key esperada del state, por ejemplo `dev/foundation.tfstate`.

Tambien calcula los CIDRs de subred usando `cidrsubnet`. La VPC por defecto es un `/16`, y cada subred se calcula como `/24`. En terminos simples: se parte una red grande en redes mas pequenas y ordenadas.

Con dos AZs, el resultado logico es:

```text
VPC: 10.0.0.0/16

Public subnets:
- eu-west-1a: 10.0.0.0/24
- eu-west-1b: 10.0.1.0/24

Private app subnets:
- eu-west-1a: 10.0.10.0/24
- eu-west-1b: 10.0.11.0/24

Private data subnets:
- eu-west-1a: 10.0.20.0/24
- eu-west-1b: 10.0.21.0/24
```

### main.tf

#### VPC

La VPC es la red privada principal dentro de AWS. En TransitOps se crea con `aws_vpc.this` y el CIDR `10.0.0.0/16` por defecto.

Se usa un `/16` porque da margen suficiente para dividir la red en varias capas: publica, app y datos, sin quedarse corto si en el futuro se necesitan mas subredes.

#### Internet Gateway

`aws_internet_gateway.this` conecta la VPC con internet. No hace que todo sea publico automaticamente; solo permite que las subredes que tengan rutas hacia el Internet Gateway puedan entrar o salir por internet.

#### Subnets publicas, app y data

Una subnet es una particion de la VPC. En este sprint hay tres capas:

- public subnets: pensadas para ALB y NAT Gateway;
- app subnets: privadas, pensadas para ECS Fargate;
- data subnets: privadas, pensadas para RDS PostgreSQL.

Las public subnets tienen `map_public_ip_on_launch = true`, porque son la capa publica donde vivira el ALB y el NAT Gateway. Las subnets de app y data no asignan IP publica.

Cada tipo de subnet se crea una vez por AZ. Con `eu-west-1a` y `eu-west-1b`, hay dos subnets publicas, dos app y dos data.

#### NAT Gateway y EIP

El NAT Gateway permite que recursos privados salgan a internet sin ser accesibles desde internet.

La diferencia importante es:

- entrada publica: internet entra por el ALB;
- salida privada: ECS puede salir a internet por el NAT para descargar imagenes desde ECR o llamar APIs de AWS.

El NAT Gateway necesita una Elastic IP (`aws_eip.nat`) porque vive en una subnet publica. En `dev` se usa un unico NAT Gateway para controlar coste.

#### Route tables

Una route table decide hacia donde va el trafico de una subnet.

Public route table:

- `0.0.0.0/0` hacia Internet Gateway.
- Asociada a las subnets publicas.
- Permite que el ALB sea alcanzable desde internet.

App route table:

- `0.0.0.0/0` hacia NAT Gateway.
- Asociada a las subnets privadas de app.
- Permite que ECS salga hacia ECR y APIs de AWS sin recibir trafico publico directo.

Data route table:

- sin ruta a internet.
- Asociada a las subnets privadas de datos.
- RDS no necesita salida publica ni entrada publica.

#### Security Groups

Un security group es un firewall virtual aplicado a recursos AWS. Define que trafico entra y que trafico sale.

ALB security group:

- ingress TCP `443` desde `0.0.0.0/0`: permite HTTPS publico.
- ingress TCP `80` desde `0.0.0.0/0`: permite HTTP publico, pensado para redirigir a HTTPS.
- egress TCP `8080` hacia el security group de ECS: el ALB solo puede reenviar al puerto interno de la API.

ECS security group:

- ingress TCP `8080` desde el security group del ALB: ECS solo acepta trafico que venga del ALB.
- egress TCP `5432` hacia el security group de RDS: ECS puede conectar con PostgreSQL.
- egress TCP `443` hacia `0.0.0.0/0`: ECS puede salir por HTTPS para pulls de ECR y llamadas a APIs de AWS.

RDS security group:

- ingress TCP `5432` desde el security group de ECS: PostgreSQL solo acepta conexiones desde las tareas ECS.
- no hay egress explicito porque la base de datos no necesita salida a internet en esta foundation.

### outputs.tf

El modulo expone valores que otros sprints necesitaran:

- identificadores de VPC y subnets;
- IDs de Internet Gateway y NAT Gateway;
- IDs de los security groups de ALB, ECS y RDS;
- tags, name prefix, region, AZs y prefijos de configuracion/secrets.

Estos outputs son importantes porque Sprint 3 podra crear ECS, RDS, ECR, ALB y CloudWatch reutilizando la red y los security groups ya definidos.

### Diagrama de red

> Este diagrama muestra la arquitectura objetivo completa. Route53, ACM y ALB se definen en Sprint 3. Sprint 2 establece la red sobre la que esos recursos se colocaran.

```text
                         Internet
                             |
                             v
                    +----------------+
                    | Route53 / ACM  |  <- Sprint 3
                    +----------------+
                             |
                             v
                    +----------------+
                    | Public ALB     |  <- Sprint 3
                    | SG: alb-sg     |
                    +----------------+
                             |
             HTTPS/HTTP      | forwards TCP 8080
                             v
+-------------------------------------------------------------------+
| VPC: 10.0.0.0/16                                                  |
|                                                                   |
|  AZ eu-west-1a                         AZ eu-west-1b              |
|  ----------------                       ----------------          |
|                                                                   |
|  Public layer                           Public layer              |
|  10.0.0.0/24                            10.0.1.0/24               |
|  ALB / NAT                              ALB-capable               |
|      |                                                            |
|      | app egress via NAT                                         |
|      v                                                            |
|  App layer                              App layer                 |
|  10.0.10.0/24                           10.0.11.0/24              |
|  ECS tasks                              ECS tasks                 |
|  SG: ecs-sg                             SG: ecs-sg                |
|      |                                                            |
|      | PostgreSQL TCP 5432                                        |
|      v                                                            |
|  Data layer                             Data layer                |
|  10.0.20.0/24                           10.0.21.0/24              |
|  RDS PostgreSQL                         RDS PostgreSQL            |
|  SG: rds-sg                             SG: rds-sg                |
|                                                                   |
+-------------------------------------------------------------------+

Routing:
- public subnets -> Internet Gateway
- app subnets    -> NAT Gateway
- data subnets   -> no internet route
```

## 5. Environments dev y prod

Los environments son los puntos de entrada que invocan el modulo. No duplican la red a mano; llaman a `platform_foundation`.

En `dev/main.tf`:

```hcl
module "platform_foundation" {
  source = "../../modules/platform_foundation"

  project_slug       = var.project_slug
  service_slug       = var.service_slug
  environment        = "dev"
  aws_region         = var.aws_region
  owner              = var.owner
  repository         = var.repository
  root_domain        = var.root_domain
  availability_zones = var.availability_zones
  tags_override      = var.tags_override
}
```

`prod/main.tf` hace lo mismo, pero con `environment = "prod"`.

Valores concretos actuales:

- region: `eu-west-1`;
- AZs: `eu-west-1a`, `eu-west-1b`;
- project slug: `transitops`;
- service slug: `api`;
- owner: `pey`;
- repository: `pey-45/TransitOps`;
- root domain: vacio hasta tener dominio real.

Los outputs de `dev` se agrupan en:

- `foundation`: convenciones, tags, hostname, prefixes y state key;
- `network`: VPC, subnets, Internet Gateway y NAT Gateway;
- `security_groups`: IDs de ALB, ECS y RDS.

Sprint 3 consumira especialmente `network` y `security_groups` para colocar:

- ALB en public subnets;
- ECS en app subnets;
- RDS en data subnets;
- reglas de comunicacion ya alineadas con ALB -> ECS -> RDS.

## 6. Flujo de uso completo

El orden correcto es:

1. Ejecutar `terraform apply` en `bootstrap/remote_state` una sola vez. Ese root usa estado local intencionalmente para crear el bucket S3 y la tabla DynamoDB.

```powershell
cd infra\terraform\bootstrap\remote_state
terraform init
terraform plan
terraform apply
```

2. Leer los outputs del bootstrap:

- `terraform_state_bucket_name`;
- `terraform_lock_table_name`;
- `dev_backend_config`;
- `prod_backend_config`.

3. Copiar `backend.hcl.example` a `backend.hcl` en el entorno correspondiente y rellenarlo con esos valores.

```powershell
cd infra\terraform\environments\dev
Copy-Item backend.hcl.example backend.hcl
```

4. Inicializar el entorno usando el backend remoto:

```powershell
terraform init -backend-config=backend.hcl
```

5. Validar formato y sintaxis:

```powershell
terraform fmt -recursive
terraform validate
```

6. Revisar y aplicar la infraestructura del entorno:

```powershell
terraform plan
terraform apply
```

Desde ese momento, el state de `dev` ya no dependera de un fichero local: vivira en S3 y usara DynamoDB para locking.

## 7. Por que el Sprint 2 no aplica nada en AWS todavia

La progresion es intencional. Sprint 2 construye la foundation como codigo: remote state, VPC, subnets, rutas, NAT, Internet Gateway y security groups. El objetivo es que el diseno este codificado, validado y entendido antes de crear recursos reales.

Sprint 3 define la capa runtime encima de esa base: ECR, CloudWatch, RDS, ECS, ALB, Route53, ACM y configuracion de secrets. Es decir, usa la foundation para colocar la aplicacion en una plataforma real.

Sprint 4 sera el momento adecuado para empezar a ejecutar el flujo completo con mas seguridad: bootstrap, backend remoto, plan y apply. Asi se evita crear recursos a ciegas antes de entender que se va a desplegar, que costes puede tener y como se destruye o reproduce.

El resultado de Sprint 2 no es una cuenta AWS con recursos creados; es una base Terraform validada y preparada para convertirse en infraestructura real cuando el proyecto este listo para aplicar.
