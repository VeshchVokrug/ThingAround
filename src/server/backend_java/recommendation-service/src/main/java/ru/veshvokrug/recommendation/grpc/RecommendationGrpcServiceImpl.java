package ru.veshvokrug.recommendation.grpc;

import io.grpc.Status;
import io.grpc.stub.StreamObserver;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Service;
import recommendation.protos.*;
import ru.veshvokrug.recommendation.event.RecommendationEventDto;
import ru.veshvokrug.recommendation.service.RecommendationService;

import java.util.List;

/**
 * gRPC сервис рекомендаций для интеграции с внешними микросервисами (C#, Node.js).
 * Предоставляет RPC методы для получения рекомендаций и публикации событий.
 *
 * @author Dmitrii Marchenko
 */
@Service
public class RecommendationGrpcServiceImpl extends RecommendationServiceGrpc.RecommendationServiceImplBase {
    private static final Logger log = LoggerFactory.getLogger(RecommendationGrpcServiceImpl.class);

    private final RecommendationService recommendationService;
    private final KafkaTemplate<String, RecommendationEventDto> kafkaTemplate;

    public RecommendationGrpcServiceImpl(
            RecommendationService recommendationService,
            KafkaTemplate<String, RecommendationEventDto> kafkaTemplate) {
        this.recommendationService = recommendationService;
        this.kafkaTemplate = kafkaTemplate;
    }

    /**
     * Получить рекомендации для пользователя.
     */
    @Override
    public void getRecommendations(GetRecommendationsRequest request,
                                    StreamObserver<GetRecommendationsResponse> responseObserver) {
        try {
            String userId = request.getUserId();
            int size = request.getSize();

            // Валидация
            if (userId.isBlank()) {
                responseObserver.onError(
                        Status.INVALID_ARGUMENT
                                .withDescription("userId must not be empty")
                                .asException()
                );
                return;
            }

            log.debug("gRPC: Getting recommendations for userId={}, size={}", userId, size);

            // Получить рекомендации
            List<String> recommendations = recommendationService.getRecommendations(userId, size);

            // Построить ответ
            GetRecommendationsResponse response = GetRecommendationsResponse.newBuilder()
                    .setUserId(userId)
                    .addAllListings(recommendations)
                    .setCount(recommendations.size())
                    .setTimestamp(System.currentTimeMillis())
                    .build();

            responseObserver.onNext(response);
            responseObserver.onCompleted();

            log.debug("gRPC: Successfully returned {} recommendations for userId={}",
                    recommendations.size(), userId);

        } catch (Exception e) {
            log.error("gRPC: Error in getRecommendations", e);
            responseObserver.onError(
                    Status.INTERNAL
                            .withDescription("Internal server error: " + e.getMessage())
                            .withCause(e)
                            .asException()
            );
        }
    }

    /**
     * Опубликовать событие рекомендаций в Kafka.
     * Используется для интеграции с C# сервисом и другими микросервисами.
     */
    @Override
    public void publishRecommendationEvent(PublishRecommendationEventRequest request,
                                           StreamObserver<PublishRecommendationEventResponse> responseObserver) {
        try {
            // Валидация
            if (request.getEventId() == null || request.getEventId().isBlank()) {
                responseObserver.onError(
                        Status.INVALID_ARGUMENT
                                .withDescription("eventId must not be empty")
                                .asException()
                );
                return;
            }

            if (request.getUserId() == null || request.getUserId().isBlank()) {
                responseObserver.onError(
                        Status.INVALID_ARGUMENT
                                .withDescription("userId must not be empty")
                                .asException()
                );
                return;
            }

            request.getEventType();
            if (request.getEventType().isBlank()) {
                responseObserver.onError(
                        Status.INVALID_ARGUMENT
                                .withDescription("eventType must not be empty")
                                .asException()
                );
                return;
            }

            request.getCategorySlug();
            if (request.getCategorySlug().isBlank()) {
                responseObserver.onError(
                        Status.INVALID_ARGUMENT
                                .withDescription("categorySlug must not be empty")
                                .asException()
                );
                return;
            }

            if (request.getTimestamp() <= 0) {
                responseObserver.onError(
                        Status.INVALID_ARGUMENT
                                .withDescription("timestamp must be positive")
                                .asException()
                );
                return;
            }

            String listingId = request.hasListingId() ? request.getListingId() : null;

            log.debug("gRPC: Publishing event eventId={}, userId={}, eventType={}",
                    request.getEventId(), request.getUserId(), request.getEventType());

            // Создать DTO события
            RecommendationEventDto event = new RecommendationEventDto(
                    request.getEventId(),
                    request.getUserId(),
                    request.getEventType(),
                    request.getCategorySlug(),
                    listingId,
                    request.getTimestamp()
            );

            // Отправить в Kafka
            kafkaTemplate.send(
                    "recommendation_events",
                    request.getUserId(),
                    event
            );

            // Построить успешный ответ
            PublishRecommendationEventResponse response = PublishRecommendationEventResponse.newBuilder()
                    .setSuccess(true)
                    .setMessage("Event published successfully")
                    .build();

            responseObserver.onNext(response);
            responseObserver.onCompleted();

            log.debug("gRPC: Event published successfully: eventId={}", request.getEventId());

        } catch (Exception e) {
            log.error("gRPC: Error in publishRecommendationEvent", e);
            responseObserver.onError(
                    Status.INTERNAL
                            .withDescription("Internal server error: " + e.getMessage())
                            .withCause(e)
                            .asException()
            );
        }
    }
}

