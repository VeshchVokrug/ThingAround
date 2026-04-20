# Build stage - многостадийная сборка для оптимизации размера образа
FROM maven:3.9.9-eclipse-temurin-25 AS builder

WORKDIR /build

# Copy pom.xml and download dependencies (для кэширования слоев)
COPY pom.xml .
RUN mvn dependency:go-offline -DdownloadSources -DdownloadJavadoc

# Copy source code
COPY src ./src

# Build application
RUN mvn clean package -DskipTests

# Runtime stage - образ для запуска
FROM eclipse-temurin:25-jre-alpine

WORKDIR /app

# Metadata
LABEL maintainer="veshvokrug.ru"
LABEL description="CoOwnership Service - управление совместной собственностью"
LABEL version="0.0.1"

# Copy jar from builder
COPY --from=builder /build/target/coownership-service-*.jar app.jar

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD java -cp app.jar org.springframework.boot.loader.Main --spring.application.name=coownership-service || exit 1

# Expose port
EXPOSE 6767

# Default environment variables
ENV SPRING_DATASOURCE_URL=jdbc:postgresql://postgres:5432/coownership
ENV SPRING_DATASOURCE_USERNAME=admin
ENV SPRING_DATASOURCE_PASSWORD=admin
ENV SPRING_JPA_HIBERNATE_DDL_AUTO=validate
ENV JAVA_OPTS="-Xms128m -Xmx256m"

# Run application
ENTRYPOINT ["java", "-jar", "app.jar"]

