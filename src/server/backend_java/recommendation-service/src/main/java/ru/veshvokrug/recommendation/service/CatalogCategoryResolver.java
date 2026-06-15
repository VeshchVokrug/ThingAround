package ru.veshvokrug.recommendation.service;

import catalog.protos.CatalogServiceGrpc;
import catalog.protos.GetRentalListingRequest;
import catalog.protos.RentalListing;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.Optional;
import java.util.concurrent.TimeUnit;

/**
 * Резолвер категории листинга через gRPC catalog-service.
 *
 * Saga-события аренды не содержат категорию, а веса рекомендаций
 * считаются по categorySlug — поэтому категория дотягивается из каталога
 * в момент первого события бронирования.
 *
 * @author Dmitrii Marchenko
 */
@Service
public class CatalogCategoryResolver {
    private static final Logger log = LoggerFactory.getLogger(CatalogCategoryResolver.class);
    private static final long DEADLINE_SECONDS = 2;

    private final CatalogServiceGrpc.CatalogServiceBlockingStub catalogStub;

    public CatalogCategoryResolver(CatalogServiceGrpc.CatalogServiceBlockingStub catalogStub) {
        this.catalogStub = catalogStub;
    }

    /**
     * @param listingId UUID rental-листинга в каталоге
     * @return categorySlug или {@link Optional#empty()}, если каталог
     * недоступен или листинг не найден
     */
    public Optional<String> resolveCategorySlug(String listingId) {
        try {
            RentalListing listing = catalogStub
                    .withDeadlineAfter(DEADLINE_SECONDS, TimeUnit.SECONDS)
                    .getRentalListing(GetRentalListingRequest.newBuilder()
                            .setListingId(listingId)
                            .build());
            String categorySlug = listing.getCategorySlug();
            return categorySlug == null || categorySlug.isBlank()
                    ? Optional.empty()
                    : Optional.of(categorySlug);
        } catch (Exception e) {
            log.warn("Не удалось получить категорию листинга {} из каталога: {}", listingId, e.getMessage());
            return Optional.empty();
        }
    }
}
