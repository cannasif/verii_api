pipeline {
  agent any

  parameters {
    string(name: 'PUBLISH_PATH', defaultValue: 'C:\\inetpub\\wwwroot\\verii-api', description: 'IIS publish hedef klasörü')
    string(name: 'APP_POOL_NAME', defaultValue: 'verii-api', description: 'IIS AppPool adı')
    booleanParam(name: 'APPLY_MIGRATIONS', defaultValue: false, description: 'Publish öncesi EF migration uygula')
  }

  environment {
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_NOLOGO = '1'
    ASPNETCORE_ENVIRONMENT = 'Production'
  }

  stages {
    stage('Restore') {
      steps {
        bat 'dotnet restore verii_api.sln'
      }
    }

    stage('Build') {
      steps {
        bat 'dotnet build verii_api.sln --configuration Release --no-restore'
      }
    }

    stage('Database Migration') {
      when {
        expression { return params.APPLY_MIGRATIONS }
      }
      steps {
        bat 'dotnet ef database update --project verii-api.csproj --startup-project verii-api.csproj --configuration Release'
      }
    }

    stage('Ensure AppPool Stopped') {
      steps {
        powershell '''
          Import-Module WebAdministration
          if (Test-Path "IIS:\\AppPools\\$env:APP_POOL_NAME") {
            $state = (Get-WebAppPoolState -Name $env:APP_POOL_NAME).Value
            if ($state -ne "Stopped") {
              Stop-WebAppPool -Name $env:APP_POOL_NAME
            }
          }
        '''
      }
    }

    stage('Publish') {
      steps {
        powershell '''
          $tempPath = Join-Path $env:WORKSPACE "publish-output"
          if (Test-Path $tempPath) {
            Remove-Item -Recurse -Force $tempPath
          }
          New-Item -ItemType Directory -Force -Path $tempPath | Out-Null
          dotnet publish verii-api.csproj --configuration Release --no-build --output $tempPath
          if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
          }

          if (!(Test-Path $env:PUBLISH_PATH)) {
            New-Item -ItemType Directory -Force -Path $env:PUBLISH_PATH | Out-Null
          }
          if (!(Test-Path (Join-Path $env:PUBLISH_PATH "logs"))) {
            New-Item -ItemType Directory -Force -Path (Join-Path $env:PUBLISH_PATH "logs") | Out-Null
          }

          robocopy $tempPath $env:PUBLISH_PATH /MIR /XD logs /NFL /NDL /NJH /NJS /NP
          if ($LASTEXITCODE -le 7) {
            exit 0
          }

          exit $LASTEXITCODE
        '''
      }
    }

    stage('Start AppPool') {
      steps {
        powershell '''
          Import-Module WebAdministration
          if (Test-Path "IIS:\\AppPools\\$env:APP_POOL_NAME") {
            Start-WebAppPool -Name $env:APP_POOL_NAME
          }
        '''
      }
    }

    stage('Archive') {
      steps {
        archiveArtifacts artifacts: '**/verii-api.dll', fingerprint: true, allowEmptyArchive: true
      }
    }
  }

  post {
    failure {
      powershell '''
        Import-Module WebAdministration
        if (Test-Path "IIS:\\AppPools\\$env:APP_POOL_NAME") {
          Start-WebAppPool -Name $env:APP_POOL_NAME
        }
      '''
    }
  }
}
