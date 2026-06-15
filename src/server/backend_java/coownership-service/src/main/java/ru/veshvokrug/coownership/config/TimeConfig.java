package ru.veshvokrug.coownership.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.time.Clock;

/**
 * Конфигурация источника времени для приложения.
 * <p>
 * Выделенный {@link Clock} упрощает тестирование логики, зависящей от текущей даты.
 *
 * @author Dmitrii Marchenko 19.04.2026
 */
@Configuration
public class TimeConfig {
    @Bean
    public Clock clock() {
        return Clock.systemUTC();
    }
}
