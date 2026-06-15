package ru.veshvokrug.recommendation;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableScheduling;

/**
 * Главный класс приложения сервиса рекомендаций.
 * Обрабатывает события активности из RabbitMQ и предоставляет персональные рекомендации.
 *
 * @author Dmitrii Marchenko
 */
@SpringBootApplication
@EnableScheduling
public class RecommendationServiceApplication {

	public static void main(String[] args) {
		SpringApplication.run(RecommendationServiceApplication.class, args);
	}

}
