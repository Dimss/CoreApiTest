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
                        def testMonogUserPass = "app"
                        def testMongoDB = "coreapitestdb"
                        def mongoServiceName = "mongodb-${getGitCommitShortHash()}"
                        openshift.withProject() {
                            def models = openshift.process( "openshift//mongodb-ephemeral",
                              "-p=DATABASE_SERVICE_NAME=${mongoServiceName}",
                              "-p=MONGODB_USER=${testMonogUserPass}",
                              "-p=MONGODB_PASSWORD=${testMonogUserPass}",
                              "-p=MONGODB_DATABASE=${testMongoDB}")
                            echo "${JsonOutput.prettyPrint(JsonOutput.toJson(models))}"
                            openshift.create(models)
                            def dc = openshift.selector("dc/${mongoServiceName}")
                            dc.untilEach(1) {
                               echo "${it.object()}"
                               return it.object().status.readyReplicas == 1
                           }

                        }
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
                        // def testDepTemplate = readFile('ocp/ci/unittests-resources-template.yaml')
                        // def models = openshift.process(testDepTemplate, "-p=RABBITMQ_NAME=${env.rabbitmqName}")
                        // openshift.delete(models)

                    }
                }
            }
        }
    }
}
