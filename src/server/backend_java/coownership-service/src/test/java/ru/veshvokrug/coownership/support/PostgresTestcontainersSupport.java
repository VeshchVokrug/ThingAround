package ru.veshvokrug.coownership.support;

import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.testcontainers.containers.PostgreSQLContainer;

/**
 * Общая база для интеграционных тестов на реальном PostgreSQL через Testcontainers.
 *
 * Контейнер объявлен в отдельном классе-держателе (не через @Container/@Testcontainers),
 * чтобы JVM держала ровно один инстанс на весь прогон тестов и Ryuk не убивал
 * его между тестовыми классами. При @Container в базовом классе каждый тестовый класс
 * создаёт и останавливает свой контейнер, из-за чего следующий класс подключается
 * к уже закрытому порту.
 */
@SuppressWarnings("resource")
public abstract class PostgresTestcontainersSupport {

    private static final PostgreSQLContainer<?> POSTGRES;

    static {
        POSTGRES = new PostgreSQLContainer<>("postgres:17-alpine")
                .withDatabaseName("coownership")
                .withUsername("test")
                .withPassword("test")
                .withReuse(false);
        POSTGRES.start();
    }

    @DynamicPropertySource
    static void registerProperties(DynamicPropertyRegistry registry) {
        registry.add("spring.datasource.url", POSTGRES::getJdbcUrl);
        registry.add("spring.datasource.username", POSTGRES::getUsername);
        registry.add("spring.datasource.password", POSTGRES::getPassword);
        registry.add("spring.datasource.driver-class-name", POSTGRES::getDriverClassName);
        // gRPC-сервер в тестах не нужен: биндит порт 9091 и падает, если
        // compose-стек уже запущен
        registry.add("grpc.server.enabled", () -> "false");
    }
}
