import groovy.json.JsonOutput

def getJobName() {
    def jobNameList = env.JOB_NAME.split("/")
    if (jobNameList.size() > 0) {
        return jobNameList[jobNameList.size() - 1]
    } else {
        return jobName
    }
}

def getAppName() {
    if (env.gitlabActionType == "TAG_PUSH") {
        return "${getJobName()}-${getGitTag()}"
    } else {
        return "${getJobName()}-${getGitCommitShortHash()}"
    }
}

def getGitCommitShortHash() {
    return checkout(scm).GIT_COMMIT.substring(0, 7)
}

def getGitTag() {
    if (env.gitlabActionType == "TAG_PUSH") {
        def tagPathList = env.gitlabSourceBranch.split("/")
        return tagPathList[tagPathList.size() - 1]
    } else {
        return ""
    }
}

def getMongoServiceName(){
    return "mongodb-${getGitCommitShortHash()}"
}

def getMongoUserAndPass(){
    return "app"
}

def getMongoDbName(){
    return "coreapitestdb"
}

def getCiInfraDeps() {

    def testMongoDB = "coreapitestdb"
    def models = openshift.process( "openshift//mongodb-ephemeral",
      "-p=DATABASE_SERVICE_NAME=${getMongoServiceName()}",
      "-p=MONGODB_USER=${getMongoUserAndPass()}",
      "-p=MONGODB_PASSWORD=${getMongoUserAndPass()}",
      "-p=MONGODB_DATABASE=${getMongoDbName()}")
    echo "${JsonOutput.prettyPrint(JsonOutput.toJson(models))}"
    return models
}

def getDockerImageTag() {
    if (env.gitlabActionType == "TAG_PUSH") {
        return getGitTag()
    } else {
        return "${getGitCommitShortHash()}-${currentBuild.number}"
    }
}

pipeline {
    agent {
        node {
            label 'dotnet22'
        }
    }
    stages {
        stage('Checkout GIT Tag (in case it was pushed) ') {

            steps {
                script {
                    if (env.gitlabActionType == "TAG_PUSH") {
                        checkout poll: false, scm: [
                                $class                           : 'GitSCM',
                                branches                         : [[name: "${env.gitlabSourceBranch}"]],
                                doGenerateSubmoduleConfigurations: false,
                                submoduleCfg                     : [],
                        ]
                    }
                }
            }
        }

        stage("Deploy tests infra dependencies") {
            steps {
                script {
                    openshift.withCluster() {
                        openshift.withProject() {
                            openshift.create(getCiInfraDeps())
                            def dc = openshift.selector("dc/${getMongoServiceName()}")
                            dc.untilEach(1) {
                               echo "${it.object()}"
                               return it.object().status.readyReplicas == 1
                           }
                        }
                    }
                }
            }
        }

        stage("Run unit tests") {
            steps {
                script {
                    sh """
                    export ControllerSettings__DbConfig__DbConnectionString=mongodb://${getMongoUserAndPass()}:${getMongoUserAndPass()}@${getMongoServiceName()}:27017
                    export ControllerSettings__DbConfig__DbName=${getMongoDbName()}
                    cd app.tests && dotnet test
                    """
                }
            }
        }
    }


    post {
        failure {
            script {
                openshift.withCluster() {
                    openshift.withProject() {
                        openshift.delete(getCiInfraDeps())
                    }
                }
            }
        }
    }
}
