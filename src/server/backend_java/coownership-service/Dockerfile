# syntax=docker/dockerfile:1.7
FROM maven:3.9.11-eclipse-temurin-25 AS builder

WORKDIR /build

COPY backend_java/coownership-service/pom.xml .
RUN --mount=type=cache,target=/root/.m2 mvn -B -ntp dependency:go-offline

COPY backend_java/coownership-service/src ./src
RUN --mount=type=cache,target=/root/.m2 mvn -B -ntp clean package -DskipTests

FROM eclipse-temurin:25-jre-alpine

WORKDIR /app

LABEL maintainer="veshvokrug.ru"
LABEL description="CoOwnership Service - управление совместной собственностью"
LABEL version="0.0.1"

COPY --from=builder /build/target/coownership-service-*.jar app.jar

EXPOSE 8085
EXPOSE 9091

ENV SPRING_DATASOURCE_URL=jdbc:postgresql://db:5432/coownership_db
ENV SPRING_DATASOURCE_USERNAME=admin
ENV SPRING_DATASOURCE_PASSWORD=admin
ENV SPRING_RABBITMQ_HOST=rabbitmq
ENV SPRING_RABBITMQ_PORT=5672
ENV SPRING_RABBITMQ_USERNAME=admin
ENV SPRING_RABBITMQ_PASSWORD=admin
ENV GRPC_SERVER_PORT=9091
ENV JAVA_OPTS="-Xms128m -Xmx256m"

ENTRYPOINT ["sh", "-c", "exec java $JAVA_OPTS -jar app.jar"]