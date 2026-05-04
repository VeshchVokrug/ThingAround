package ru.veshvokrug.coownership.input.grpc;

import io.grpc.Status;
import io.grpc.StatusRuntimeException;
import ru.veshvokrug.coownership.service.ServiceException;

/**
 * Маппер доменных исключений в gRPC status.
 *
 * @author Dmitrii Marchenko 25.04.2026
 */
public final class GrpcExceptionMapper {
    private GrpcExceptionMapper() {
    }

    public static StatusRuntimeException toStatus(Throwable throwable) {
        if (throwable instanceof StatusRuntimeException statusRuntimeException) {
            return statusRuntimeException;
        }

        if (throwable instanceof ServiceException serviceException) {
            Status status = switch (serviceException.getCode()) {
                case BAD_REQUEST -> Status.INVALID_ARGUMENT;
                case FORBIDDEN -> Status.PERMISSION_DENIED;
                case NOT_FOUND -> Status.NOT_FOUND;
                case CONFLICT -> Status.FAILED_PRECONDITION;
            };
            String description = serviceException.getMessage() == null
                    ? "Service error"
                    : serviceException.getMessage();
            return status.withDescription(description).asRuntimeException();
        }

        if (throwable instanceof IllegalArgumentException illegalArgumentException) {
            return Status.INVALID_ARGUMENT
                    .withDescription(illegalArgumentException.getMessage())
                    .asRuntimeException();
        }

        return Status.INTERNAL
                .withDescription("Unexpected internal error")
                .withCause(throwable)
                .asRuntimeException();
    }
}
