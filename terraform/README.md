# Terraform & Terragrunt — SecurityRule

## Структура

```
terragrunt/
├── terragrunt.hcl                   ← Корневой конфиг: remote state (Azure Blob), провайдер
├── _envcommon/
│   ├── k8s-cluster.hcl              ← Общие inputs для модуля k8s-cluster
│   └── k8s-app.hcl                  ← Общие inputs для модуля k8s-app
└── environments/
    ├── dev/
    │   ├── env.hcl                  ← Переменные dev (environment=dev, location)
    │   ├── k8s-cluster/
    │   │   └── terragrunt.hcl       ← Dev: 1 нода, Standard_B2s
    │   └── k8s-app/
    │       └── terragrunt.hcl       ← Dev: 1 реплика
    └── prod/
        ├── env.hcl                  ← Переменные prod (environment=prod)
        ├── k8s-cluster/
        │   └── terragrunt.hcl       ← Prod: 3 ноды, Standard_D4s_v3
        └── k8s-app/
            └── terragrunt.hcl       ← Prod: 2 реплики, стабильный тег образа

terraform/
└── modules/
    ├── k8s-cluster/                 ← Провижининг AKS (main/variables/outputs)
    └── k8s-app/                     ← Деплой приложения в k8s (main/variables/outputs)
```

## Предварительные требования

- [Terraform](https://developer.hashicorp.com/terraform/downloads) ≥ 1.6
- [Terragrunt](https://terragrunt.gruntwork.io/docs/getting-started/install/) ≥ 0.55
- Azure CLI (`az login`) — если используете Azure AKS
- Созданный storage account для remote state:
  ```bash
  az group create -n rg-securityrule-tfstate -l westeurope
  az storage account create -n stsecurityruletf -g rg-securityrule-tfstate --sku Standard_LRS
  az storage container create -n tfstate --account-name stsecurityruletf
  ```

## Быстрый старт

### Создание кластера (dev)
```bash
cd terragrunt/environments/dev/k8s-cluster
terragrunt init
terragrunt plan
terragrunt apply
```

### Деплой приложения (dev)
```bash
# Передать секреты через переменные окружения:
export TF_VAR_db_connection_string="Server=mssql;Database=SecurityRuleDb;User Id=sa;Password=YourPass!;TrustServerCertificate=True;"
export TF_VAR_fakead_connection_string="Server=mssql;Database=FakeAdDb;User Id=sa;Password=YourPass!;TrustServerCertificate=True;"
export TF_VAR_mssql_sa_password="YourStrong!Passw0rd"

cd terragrunt/environments/dev/k8s-app
terragrunt init
terragrunt plan
terragrunt apply
```

### Весь стек сразу (run-all)
```bash
cd terragrunt/environments/dev
terragrunt run-all apply
```

### Prod
```bash
cd terragrunt/environments/prod
terragrunt run-all plan   # проверить перед применением
terragrunt run-all apply
```

## Смена облачного провайдера

По умолчанию используется **Azure AKS**. Для смены провайдера замените:

| Провайдер | `modules/k8s-cluster/main.tf` | `terragrunt.hcl` backend |
|-----------|-------------------------------|--------------------------|
| AWS EKS   | `aws_eks_cluster`             | `s3`                     |
| GCP GKE   | `google_container_cluster`    | `gcs`                    |
| On-prem   | `kind` / `kubeadm` (вне TF)   | `local`                  |

## Секреты

Никогда не хардкодьте пароли в `.hcl` файлах. Рекомендуемые подходы:
- **CI/CD**: передавать через `TF_VAR_*` переменные окружения из секретов pipeline
- **Azure Key Vault**: использовать `azurerm_key_vault_secret` data source
- **HashiCorp Vault**: использовать Terragrunt `sops` или `vault` провайдер
