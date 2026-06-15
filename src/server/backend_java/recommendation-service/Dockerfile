# syntax=docker/dockerfile:1.7
FROM maven:3.9.11-eclipse-temurin-25 AS builder

WORKDIR /build

COPY backend_java/recommendation-service/pom.xml .
RUN --mount=type=cache,target=/root/.m2 mvn -B -ntp dependency:go-offline

COPY backend_java/recommendation-service/src ./src
RUN --mount=type=cache,target=/root/.m2 mvn -B -ntp clean package -DskipTests

FROM eclipse-temurin:25-jre-alpine

WORKDIR /app

LABEL maintainer="veshvokrug.ru"
LABEL description="Recommendation Service - сервис рекомендаций"
LABEL version="0.0.1"

COPY --from=builder /build/target/recommendation-service-*.jar app.jar

EXPOSE 8084
EXPOSE 50054

ENV SPRING_RABBITMQ_HOST=rabbitmq
ENV SPRING_RABBITMQ_PORT=5672
ENV SPRING_RABBITMQ_USERNAME=admin
ENV SPRING_RABBITMQ_PASSWORD=admin
ENV SPRING_DATA_REDIS_HOST=redis
ENV SPRING_DATA_REDIS_PORT=6379
ENV GRPC_SERVER_PORT=50054
ENV JAVA_OPTS="-Xms128m -Xmx256m"

ENTRYPOINT ["sh", "-c", "exec java $JAVA_OPTS -jar app.jar"]