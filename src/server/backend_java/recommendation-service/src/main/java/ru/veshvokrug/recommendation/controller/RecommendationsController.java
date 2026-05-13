package ru.veshvokrug.recommendation.controller;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import ru.veshvokrug.recommendation.controller.dto.RecommendationResponse;
import ru.veshvokrug.recommendation.service.RecommendationService;

import java.util.List;

/**
 * REST-контроллер для эндпоинтов рекомендаций.
 * Возвращает персональные рекомендации по объявлениям.
 *
 * @author Dmitrii Marchenko
 */
@RestController
@RequestMapping("/api/v1/recommendations")
public class RecommendationsController {
    private static final Logger logger = LoggerFactory.getLogger(RecommendationsController.class);

    private final RecommendationService recommendationService;

    public RecommendationsController(RecommendationService recommendationService) {
        this.recommendationService = recommendationService;
    }

    /**
     * Возвращает персональные рекомендации для пользователя.
     *
     * @param userId идентификатор пользователя
     * @param size количество рекомендаций (по умолчанию берётся из конфигурации)
     * @return список идентификаторов рекомендованных объявлений
     */
    @GetMapping
    public ResponseEntity<RecommendationResponse> getRecommendations(
            @RequestParam String userId,
            @RequestParam(defaultValue = "0") int size) {

        logger.debug("Получен запрос рекомендаций: userId={}, size={}", userId, size);

        if (userId == null || userId.isBlank()) {
            return ResponseEntity.badRequest().build();
        }

        List<String> listings = recommendationService.getRecommendations(userId, size);

        RecommendationResponse response = new RecommendationResponse(
                userId,
                listings,
                listings.size(),
                System.currentTimeMillis()
        );

        logger.debug("Возвращаю {} рекомендаций для пользователя {}", listings.size(), userId);
        return ResponseEntity.ok(response);
    }

}

