pipeline {
  agent any

  environment {
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_NOLOGO = '1'
    ASPNETCORE_ENVIRONMENT = 'Production'
    PUBLISH_DIR = 'publish/api'
  }

  stages {
    stage('Restore') {
      steps {
        sh 'dotnet restore verii_api.sln'
      }
    }

    stage('Build') {
      steps {
        sh 'dotnet build verii_api.sln --configuration Release --no-restore'
      }
    }

    stage('Database Migration') {
      when {
        expression { return env.APPLY_MIGRATIONS == 'true' }
      }
      steps {
        sh 'dotnet ef database update --project verii_api.csproj --startup-project verii_api.csproj --configuration Release'
      }
    }

    stage('Publish') {
      steps {
        sh 'rm -rf "$PUBLISH_DIR"'
        sh 'dotnet publish verii_api.csproj --configuration Release --no-build --output "$PUBLISH_DIR"'
      }
    }

    stage('Archive') {
      steps {
        archiveArtifacts artifacts: 'publish/api/**', fingerprint: true
      }
    }
  }
}
