#docker run [OPTIONS] IMAGE [COMMAND] [ARG...]

docker pull rabbitmq
docker run -d --hostname RabbitMq --name rabbitmq -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=guest -e RABBITMQ_DEFAULT_PASS=guest  rabbitmq:3-management

#docker pull rabbitmq:3-management
#docker run -d --hostname RabbitMq --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management


#add this if relevant:

#-v /temp/rabbitmq/rabbitmq-data:/var/lib/rabbitmq 

#After it’s up and running, you can point your browser to http://localhost:15672 

docker restart rabbitmq
#docker stop rabbitmq
#docker start rabbitmq
#docker kill rabbitmq
