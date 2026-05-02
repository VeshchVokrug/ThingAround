# syntax=docker/dockerfile:1.7
# Build stage - многостадийная сборка для оптимизации размера образа
FROM maven:3.9.11-eclipse-temurin-25 AS builder

WORKDIR /build

# Copy pom.xml and download dependencies (для кэширования слоев)
COPY pom.xml .
RUN --mount=type=cache,target=/root/.m2 mvn -B -ntp dependency:go-offline

# Copy source code
COPY src ./src

# Build application
RUN --mount=type=cache,target=/root/.m2 mvn -B -ntp clean package -DskipTests

# Runtime stage - образ для запуска
FROM eclipse-temurin:25-jre-alpine

WORKDIR /app

# Metadata
LABEL maintainer="veshvokrug.ru"
LABEL description="CoOwnership Service - управление совместной собственностью"
LABEL version="0.0.1"

# Copy jar from builder
COPY --from=builder /build/target/coownership-service-*.jar app.jar

# Expose port
EXPOSE 9091

# Default environment variables
ENV SPRING_DATASOURCE_URL=jdbc:postgresql://postgres:5432/coownership
ENV SPRING_DATASOURCE_USERNAME=admin
ENV SPRING_DATASOURCE_PASSWORD=admin
ENV SPRING_KAFKA_BOOTSTRAP_SERVERS=kafka:9092
ENV GRPC_SERVER_PORT=9091
ENV SPRING_JPA_HIBERNATE_DDL_AUTO=validate
ENV JAVA_OPTS="-Xms128m -Xmx256m"

# Run application
ENTRYPOINT ["sh", "-c", "exec java $JAVA_OPTS -jar app.jar"]
