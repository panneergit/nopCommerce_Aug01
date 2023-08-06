pipeline {
    agent {label 'DOT_NET_AGENT' }
    stages {
        stage('vcs') {
            steps {
                git branch: 'master', 
                    url: 'https://github.com/panneergit/nopCommerce_Aug01.git'
            }
            
        }
        stage('package') {
            steps {
                sh 'docker image build -t nopcommerce:latest .'
                sh 'docker image tag nopcommerce:latest devops_repo/nopcommerceaug01:latest'
                sh 'docker image push devops_repo/nopcommerceaug01:latest'
                
            }            
        }
        stage('deploy') {
            steps {
                sh 'cd deploy && terraform init && terraform apply -auto-approve && az aks get-credentials --resource-group rg-national-cod --name cluster-star-goat && kubectl apply -f ../k8s/nop-deploy.yaml' 
                //sh 'echo "$(terraform output kube_config)" > ./azurek8s && export KUBECONFIG=./azurek8s && kubectl apply -f ../k8s/nop-deploy.yaml'
            }
        }
    }
}