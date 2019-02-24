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

        stage("Run tests") {
            steps {
                script {
                    sh """
                        export ControllerSettings__DbConfig__DbConnectionString=mongodb://${getMongoUserAndPass()}:${getMongoUserAndPass()}@${getMongoServiceName()}:27017/${getMongoDbName()}
                        export ControllerSettings__DbConfig__DbName=${getMongoDbName()}
                        cd app.tests && dotnet test
                    """
                }
            }
        }
    }
    stage("Cleanup test resources") {
       steps {
           script {
               openshift.withCluster() {
                   openshift.withProject() {
                       openshift.delete(getCiInfraDeps())
                   }
               }
           }
       }
    }
    stage("Create S2I image stream and build configs") {
        steps {
            script {
                openshift.withCluster() {
                    openshift.withProject() {
                        def icBcTemplate = readFile('ocp/ci/app-is-bc.yaml')
                        def models = openshift.process(icBcTemplate,
                                "-p=BC_IS_NAME=${getAppName()}",
                                "-p=DOCKER_REGISTRY=${env.DOCKER_REGISTRY}",
                                "-p=DOCKER_IMAGE_NAME=/${env.DOCKER_IMAGE_PREFIX}/${GOVIL_APP_NAME}",
                                "-p=DOCKER_IMAGE_TAG=${getDockerImageTag()}",
                                "-p=GIT_REPO=${scm.getUserRemoteConfigs()[0].getUrl()}",
                                "-p=GIT_REF=${env.gitlabSourceBranch}",
                                "-p=S2I_BUILDER_ISTAG=${env.S2I_BUILD_IMAGE}"
                        )
                        echo "${JsonOutput.prettyPrint(JsonOutput.toJson(models))}"
                        openshift.create(models)
                        def bc = openshift.selector("buildconfig/${getAppName()}")
                        def build = bc.startBuild()
                        build.logs("-f")
                        openshift.delete(models)
                    }
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
