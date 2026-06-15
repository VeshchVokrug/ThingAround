package ru.veshvokrug.recommendation.controller;

import jakarta.validation.constraints.NotBlank;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.*;
import ru.veshvokrug.recommendation.controller.dto.RecommendationResponse;
import ru.veshvokrug.recommendation.event.EventType;
import ru.veshvokrug.recommendation.event.RecommendationEventDto;
import ru.veshvokrug.recommendation.publisher.RecommendationEventPublisher;
import ru.veshvokrug.recommendation.service.RecommendationService;

import java.util.List;
import java.util.UUID;

/**
 * REST контроллер для интеграции с внешними сервисами (C#, Node.js).
 * Предоставляет эндпоинты для получения рекомендаций и публикации событий.
 *
 * @author Dmitrii Marchenko
 */
@Slf4j
@RestController
@RequestMapping("/api/v2/recommendations")
@Validated
public class ExternalIntegrationController {

    private final RecommendationService recommendationService;
    private final RecommendationEventPublisher eventPublisher;

    public ExternalIntegrationController(
            RecommendationService recommendationService,
            RecommendationEventPublisher eventPublisher) {
        this.recommendationService = recommendationService;
        this.eventPublisher = eventPublisher;
    }

    /**
     * Получить рекомендации для пользователя (совместимо с C#).
     * GET /api/v2/recommendations?userId={userId}&size={size}
     */
    @GetMapping
    public ResponseEntity<RecommendationResponse> getRecommendations(
            @RequestParam @NotBlank(message = "userId must not be blank") String userId,
            @RequestParam(defaultValue = "0") int size) {

        log.debug("REST: Getting recommendations for userId={}, size={}", userId, size);

        try {
            List<String> recommendations = recommendationService.getRecommendations(userId, size);

            RecommendationResponse response = new RecommendationResponse(
                    userId,
                    recommendations,
                    recommendations.size(),
                    System.currentTimeMillis()
            );

            log.debug("REST: Successfully returned {} recommendations for userId={}",
                    recommendations.size(), userId);

            return ResponseEntity.ok(response);

        } catch (Exception e) {
            log.error("REST: Error getting recommendations for userId={}", userId, e);
            return ResponseEntity
                    .status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .build();
        }
    }

    /**
     * Опубликовать событие рекомендации из C# сервиса.
     * POST /api/v2/recommendations/events
     */
    @PostMapping("/events")
    public ResponseEntity<?> publishEvent(@RequestBody PublishEventRequest request) {
        try {
            log.debug("REST: Publishing event from C#: userId={}, eventType={}",
                    request.userId(), request.eventType());

            // Валидация
            if (request.userId() == null || request.userId().isBlank()) {
                return ResponseEntity.badRequest().body("userId must not be blank");
            }

            if (request.eventType() == null || request.eventType().isBlank()) {
                return ResponseEntity.badRequest().body("eventType must not be blank");
            }

            // Неизвестный тип молча отбрасывается консьюмером — лучше сразу
            // вернуть 400, чтобы интегрирующаяся сторона увидела опечатку
            if (EventType.fromValue(request.eventType()) == null) {
                return ResponseEntity.badRequest().body("Unknown eventType: " + request.eventType());
            }

            if (request.categorySlug() == null || request.categorySlug().isBlank()) {
                return ResponseEntity.badRequest().body("categorySlug must not be blank");
            }

            // Создать событие
            RecommendationEventDto event = new RecommendationEventDto(
                    request.eventId() != null ? request.eventId() : UUID.randomUUID().toString(),
                    request.userId(),
                    request.eventType(),
                    request.categorySlug(),
                    request.listingId(),
                    request.timestamp() > 0 ? request.timestamp() : System.currentTimeMillis()
            );

            // Отправить в RabbitMQ
            eventPublisher.publish(event);

            log.debug("REST: Event published successfully: eventId={}", event.eventId());

            return ResponseEntity.ok(new PublishEventResponse(true, "Event published successfully"));

        } catch (Exception e) {
            log.error("REST: Error publishing event", e);
            return ResponseEntity
                    .status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body("Error publishing event: " + e.getMessage());
        }
    }

    /**
     * DTO запроса на публикацию события
     */
    public record PublishEventRequest(
            String eventId,
            String userId,
            String eventType,
            String categorySlug,
            String listingId,
            long timestamp
    ) {}

    /**
     * DTO ответа на публикацию события
     */
    public record PublishEventResponse(
            boolean success,
            String message
    ) {}
}
