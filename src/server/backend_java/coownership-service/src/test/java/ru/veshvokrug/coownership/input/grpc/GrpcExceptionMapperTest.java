package ru.veshvokrug.coownership.input.grpc;

import io.grpc.Status;
import io.grpc.StatusRuntimeException;
import org.junit.jupiter.api.Test;
import ru.veshvokrug.coownership.service.ServiceException;

import static org.assertj.core.api.Assertions.assertThat;

class GrpcExceptionMapperTest {

    @Test
    void mapsConflictToFailedPrecondition() {
        StatusRuntimeException result = GrpcExceptionMapper.toStatus(
                ServiceException.conflict("conflict")
        );

        assertThat(result.getStatus().getCode()).isEqualTo(Status.Code.FAILED_PRECONDITION);
        assertThat(result.getStatus().getDescription()).isEqualTo("conflict");
    }

    @Test
    void mapsIllegalArgumentToInvalidArgument() {
        StatusRuntimeException result = GrpcExceptionMapper.toStatus(
                new IllegalArgumentException("bad request")
        );

        assertThat(result.getStatus().getCode()).isEqualTo(Status.Code.INVALID_ARGUMENT);
        assertThat(result.getStatus().getDescription()).isEqualTo("bad request");
    }
}
