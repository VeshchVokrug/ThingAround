package ru.veshvokrug.recommendation.controller.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.List;

/**
 * DTO ответа для эндпоинта рекомендаций.
 * Содержит список рекомендованных идентификаторов объявлений.
 *
 * @author Dmitrii Marchenko
 */
public record RecommendationResponse(
        @JsonProperty("userId")
        String userId,

        @JsonProperty("listings")
        List<String> listings,

        @JsonProperty("count")
        int count,

        @JsonProperty("timestamp")
        long timestamp
) {
}

