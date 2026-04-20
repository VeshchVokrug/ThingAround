package ru.veshvokrug.coownership.input.controller;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.server.ResponseStatusException;
import ru.veshvokrug.coownership.input.dto.ErrorResponseDto;

import java.time.Instant;
import java.util.stream.Collectors;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
@RestControllerAdvice
public class GlobalExceptionHandler {
    private static final Logger log = LoggerFactory
            .getLogger(GlobalExceptionHandler.class);

    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<ErrorResponseDto> handleValidation(
            MethodArgumentNotValidException e) {
        String message = e.getBindingResult().getAllErrors().stream()
                .map(error ->
                        error.getDefaultMessage() == null ?
                                "Validation failed"
                                : error.getDefaultMessage())
                .distinct()
                .collect(Collectors.joining("; "));

        ErrorResponseDto errorResponseDto = new ErrorResponseDto(
                HttpStatus.BAD_REQUEST.value(),
                message.isBlank() ? "Validation failed" : message,
                Instant.now()
        );

        return ResponseEntity
                .status(HttpStatus.BAD_REQUEST)
                .body(errorResponseDto);
    }

    @ExceptionHandler(ResponseStatusException.class)
    public ResponseEntity<ErrorResponseDto> handleResponseStatus(ResponseStatusException e) {
        HttpStatus status = HttpStatus.valueOf(e.getStatusCode().value());
        ErrorResponseDto errorResponseDto = new ErrorResponseDto(
                status.value(),
                e.getReason() == null ? "Request failed" : e.getReason(),
                Instant.now()
        );

        return ResponseEntity
                .status(status)
                .body(errorResponseDto);
    }

    @ExceptionHandler(DataIntegrityViolationException.class)
    public ResponseEntity<ErrorResponseDto> handleDataIntegrity(DataIntegrityViolationException e) {
        ErrorResponseDto errorResponseDto = new ErrorResponseDto(
                HttpStatus.CONFLICT.value(),
                "Конфликт данных: операция нарушает ограничения целостности",
                Instant.now()
        );

        return ResponseEntity
                .status(HttpStatus.CONFLICT)
                .body(errorResponseDto);
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponseDto> handleAll(
            Exception e
    ) {
        log.error("Handle exception: {}", e.getMessage(), e);

        ErrorResponseDto errorResponseDto = new ErrorResponseDto(
                HttpStatus.INTERNAL_SERVER_ERROR.value(),
                "Internal server error",
                Instant.now()
        );

        return ResponseEntity
                .status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(errorResponseDto);
    }
}
