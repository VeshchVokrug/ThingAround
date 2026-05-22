package ru.veshvokrug.recommendation.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RestController;
import ru.veshvokrug.recommendation.controller.dto.Listing;
import ru.veshvokrug.recommendation.controller.dto.ThingDto;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.List;

/**
 * @author Dmitrii Marchenko 06.04.2026
 */
@RestController
public class RecommendationsController {

    @GetMapping("/{uuid}")
    public ResponseEntity<List<ThingDto>> getThingList(@PathVariable String uuid) {
        return ResponseEntity.ok(hardcodedThings);
    }

    private static final List<ThingDto> hardcodedThings = List.of(
            new ThingDto(
                    "user-123",
                    Instant.now(),
                    false,
                    List.of(
                            new Listing(
                                    "listing-001",
                                    "Дрель Makita HP333D",
                                    "instruments",
                                    new BigDecimal("350.00"),
                                    List.of(
                                            "https://example.com/images/drill1.jpg",
                                            "https://example.com/images/drill2.jpg"
                                    ),
                                    4.8,
                                    0.95
                            ),
                            new Listing(
                                    "listing-002",
                                    "Сноуборд Burton Custom 158",
                                    "sports",
                                    new BigDecimal("800.00"),
                                    List.of(
                                            "https://example.com/images/snowboard1.jpg"
                                    ),
                                    4.9,
                                    0.92
                            ),
                            new Listing(
                                    "listing-003",
                                    "Швейная машинка Brother CX5",
                                    "home-appliances",
                                    new BigDecimal("450.00"),
                                    List.of(
                                            "https://example.com/images/sewing1.jpg",
                                            "https://example.com/images/sewing2.jpg",
                                            "https://example.com/images/sewing3.jpg"
                                    ),
                                    4.7,
                                    0.89
                            )
                    )
            ),
            new ThingDto(
                    "user-456",
                    Instant.now(),
                    true,
                    List.of(
                            new Listing(
                                    "listing-004",
                                    "Палатка Quechua Arpenaz 3",
                                    "camping",
                                    new BigDecimal("600.00"),
                                    List.of(
                                            "https://example.com/images/tent1.jpg"
                                    ),
                                    4.6,
                                    0.88
                            ),
                            new Listing(
                                    "listing-005",
                                    "Перфоратор Bosch GBH 2-26",
                                    "instruments",
                                    new BigDecimal("500.00"),
                                    List.of(
                                            "https://example.com/images/hammer1.jpg",
                                            "https://example.com/images/hammer2.jpg"
                                    ),
                                    4.9,
                                    0.94
                            ),
                            new Listing(
                                    "listing-006",
                                    "Велосипед Trek Marlin 5",
                                    "sports",
                                    new BigDecimal("900.00"),
                                    List.of(
                                            "https://example.com/images/bike1.jpg",
                                            "https://example.com/images/bike2.jpg"
                                    ),
                                    4.5,
                                    0.87
                            ),
                            new Listing(
                                    "listing-007",
                                    "Каяк надувной Intex Explorer K2",
                                    "water-sports",
                                    new BigDecimal("1200.00"),
                                    List.of(
                                            "https://example.com/images/kayak1.jpg"
                                    ),
                                    4.4,
                                    0.85
                            )
                    )
            ),
            new ThingDto(
                    "user-789",
                    Instant.now(),
                    false,
                    List.of(
                            new Listing(
                                    "listing-008",
                                    "Фотоаппарат Canon EOS 2000D",
                                    "electronics",
                                    new BigDecimal("1500.00"),
                                    List.of(
                                            "https://example.com/images/camera1.jpg",
                                            "https://example.com/images/camera2.jpg",
                                            "https://example.com/images/camera3.jpg"
                                    ),
                                    4.9,
                                    0.96
                            ),
                            new Listing(
                                    "listing-009",
                                    "Квадрокоптер DJI Mini 2",
                                    "electronics",
                                    new BigDecimal("2500.00"),
                                    List.of(
                                            "https://example.com/images/drone1.jpg"
                                    ),
                                    5.0,
                                    0.98
                            )
                    )
            )
    );
}
