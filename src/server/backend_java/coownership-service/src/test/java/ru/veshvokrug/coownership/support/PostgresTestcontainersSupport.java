package ru.veshvokrug.coownership.support;

import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.testcontainers.containers.PostgreSQLContainer;
import org.testcontainers.junit.jupiter.Container;
import org.testcontainers.junit.jupiter.Testcontainers;

/**
 * Общая база для интеграционных тестов на реальном PostgreSQL через Testcontainers.
 */
@Testcontainers
@SuppressWarnings("resource")
public abstract class PostgresTestcontainersSupport {

    @Container
    static final PostgreSQLContainer<?> POSTGRES = createPostgresContainer();

    @SuppressWarnings("resource")
    private static PostgreSQLContainer<?> createPostgresContainer() {
        return new PostgreSQLContainer<>("postgres:17-alpine")
                .withDatabaseName("coownership")
                .withUsername("test")
                .withPassword("test");
    }

    @DynamicPropertySource
    static void registerDatasourceProperties(DynamicPropertyRegistry registry) {
        registry.add("spring.datasource.url", POSTGRES::getJdbcUrl);
        registry.add("spring.datasource.username", POSTGRES::getUsername);
        registry.add("spring.datasource.password", POSTGRES::getPassword);
        registry.add("spring.datasource.driver-class-name", POSTGRES::getDriverClassName);
    }
}
