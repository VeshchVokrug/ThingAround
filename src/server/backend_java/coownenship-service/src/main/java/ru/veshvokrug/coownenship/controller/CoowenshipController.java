package ru.veshvokrug.coownenship.controller;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;
import ru.veshvokrug.coownenship.controller.dto.AnnouncementResponseDto;
import ru.veshvokrug.coownenship.controller.dto.ShareResponseDto;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;

/**
 * @author Dmitrii Marchenko 06.04.2026
 */
@RestController
public class CoowenshipController {

    @GetMapping("/announcements")
    public ResponseEntity<List<AnnouncementResponseDto>> getAnnouncementsList() {
        return ResponseEntity.ok(hardcodedAnnouncements());
    }

    private List<AnnouncementResponseDto> hardcodedAnnouncements() {
        return List.of(
                new AnnouncementResponseDto(
                        "550e8400-e29b-41d4-a716-446655440001",
                        "550e8400-e29b-41d4-a716-446655440000",
                        "3D-принтер Bambu Lab X1",
                        "Профессиональный 3D-принтер для прототипирования и хобби-проектов",
                        "electronics",
                        List.of("https://example.com/images/bambu1.jpg"),
                        new BigDecimal("120000"),
                        new BigDecimal("30000"),
                        2, 4,
                        AnnouncementStatus.OPEN,
                        LocalDate.of(2026, 6, 1),
                        4.8,
                        List.of(
                                new ShareResponseDto("share-001", 25, ShareStatus.OCCUPIED),
                                new ShareResponseDto("share-002", 25, ShareStatus.OCCUPIED),
                                new ShareResponseDto("share-003", 25, ShareStatus.VACANT),
                                new ShareResponseDto("share-004", 25, ShareStatus.VACANT)
                        )
                ),
                new AnnouncementResponseDto(
                        "550e8400-e29b-41d4-a716-446655440002",
                        "550e8400-e29b-41d4-a716-446655440000",
                        "Лодка моторная Казанка",
                        "Алюминиевая лодка с мотором 15 л.с., идеально для рыбалки",
                        "sport",
                        List.of("https://example.com/images/boat1.jpg"),
                        new BigDecimal("80000"),
                        new BigDecimal("20000"),
                        1, 4,
                        AnnouncementStatus.OPEN,
                        LocalDate.of(2026, 5, 15),
                        4.5,
                        List.of(
                                new ShareResponseDto("share-005", 25, ShareStatus.OCCUPIED),
                                new ShareResponseDto("share-006", 25, ShareStatus.VACANT),
                                new ShareResponseDto("share-007", 25, ShareStatus.VACANT),
                                new ShareResponseDto("share-008", 25, ShareStatus.VACANT)
                        )
                ),
                new AnnouncementResponseDto(
                        "550e8400-e29b-41d4-a716-446655440003",
                        "550e8400-e29b-41d4-a716-446655440000",
                        "Фотоаппарат Sony A7 IV",
                        "Полнокадровая беззеркальная камера с объективом 24-70mm",
                        "electronics",
                        List.of("https://example.com/images/sony1.jpg"),
                        new BigDecimal("60000"),
                        new BigDecimal("20000"),
                        0, 3,
                        AnnouncementStatus.OPEN,
                        LocalDate.of(2026, 5, 30),
                        4.9,
                        List.of(
                                new ShareResponseDto("share-009", 34, ShareStatus.VACANT),
                                new ShareResponseDto("share-010", 33, ShareStatus.VACANT),
                                new ShareResponseDto("share-011", 33, ShareStatus.VACANT)
                        )
                )
        );
    }
}