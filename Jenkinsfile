pipeline {
  agent any

  parameters {
    string(name: 'PUBLISH_PATH', defaultValue: 'C:\\inetpub\\wwwroot\\verii-api', description: 'IIS publish hedef klasörü')
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

    stage('Publish') {
      steps {
        bat 'if exist "%PUBLISH_PATH%" rmdir /S /Q "%PUBLISH_PATH%"'
        bat 'dotnet publish verii-api.csproj --configuration Release --no-build --output "%PUBLISH_PATH%"'
      }
    }

    stage('Archive') {
      steps {
        archiveArtifacts artifacts: '**/verii-api.dll', fingerprint: true, allowEmptyArchive: true
      }
    }
  }
}
