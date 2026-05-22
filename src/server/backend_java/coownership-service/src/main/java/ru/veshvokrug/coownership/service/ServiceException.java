package ru.veshvokrug.coownership.service;

/**
 * Доменное исключение уровня бизнес-логики.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
public class ServiceException extends RuntimeException {
    public enum Code {
        BAD_REQUEST,
        FORBIDDEN,
        NOT_FOUND,
        CONFLICT
    }

    private final Code code;

    private ServiceException(Code code, String message) {
        super(message);
        this.code = code;
    }

    public Code getCode() {
        return code;
    }

    public static ServiceException badRequest(String message) {
        return new ServiceException(Code.BAD_REQUEST, message);
    }

    public static ServiceException forbidden(String message) {
        return new ServiceException(Code.FORBIDDEN, message);
    }

    public static ServiceException notFound(String message) {
        return new ServiceException(Code.NOT_FOUND, message);
    }

    public static ServiceException conflict(String message) {
        return new ServiceException(Code.CONFLICT, message);
    }
}
