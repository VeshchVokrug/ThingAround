package ru.veshvokrug.coownership.config;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.databind.json.JsonMapper;
import org.springframework.boot.autoconfigure.condition.ConditionalOnMissingBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Конфигурация Jackson. Ключевой момент: даты сериализуются строками ISO-8601,
 * а не числами/массивами. Это критично для совместимости с System.Text.Json
 * на стороне C#-сервисов (MassTransit consumer в CatalogService).
 *
 * @author Dmitrii Marchenko 25.04.2026
 */
@Configuration
public class JacksonConfig {
    @Bean
    @ConditionalOnMissingBean(ObjectMapper.class)
    public ObjectMapper objectMapper() {
        return JsonMapper.builder()
                .findAndAddModules()
                .disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS)
                .disable(SerializationFeature.WRITE_DATE_TIMESTAMPS_AS_NANOSECONDS)
                .build();
    }
}
