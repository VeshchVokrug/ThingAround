package ru.veshvokrug.coownership.input;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import ru.veshvokrug.coownership.input.dto.ErrorResponseDto;

import java.time.Instant;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
@RestControllerAdvice
public class GlobalExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponseDto> handleAll(
            Exception e
    ) {

        log.error("Handle exception: {}", e.getMessage(), e);

        ErrorResponseDto errorResponseDto = new ErrorResponseDto(
                500,
                e.getMessage(),
                Instant.now()
        );

        return ResponseEntity
                .status(500)
                .body(errorResponseDto);
    }
}
