package ru.veshvokrug.coownership;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableScheduling;

/**
 * Точка входа Spring Boot приложения coownership-service.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@SpringBootApplication
@EnableScheduling
public class CoownershipServiceApplication {

    static void main(String[] args) {
        SpringApplication.run(CoownershipServiceApplication.class, args);
    }

}
