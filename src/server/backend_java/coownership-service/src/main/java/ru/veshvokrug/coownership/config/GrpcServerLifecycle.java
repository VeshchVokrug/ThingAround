package ru.veshvokrug.coownership.config;

import io.grpc.BindableService;
import io.grpc.Server;
import io.grpc.ServerBuilder;
import jakarta.annotation.PostConstruct;
import jakarta.annotation.PreDestroy;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.util.List;

/**
 * @author Dmitrii Marchenko 25.04.2026
 */
@Component
@ConditionalOnProperty(name = "grpc.server.enabled", havingValue = "true", matchIfMissing = true)
public class GrpcServerLifecycle {
    private static final Logger logger = LoggerFactory.getLogger(GrpcServerLifecycle.class);

    private final int port;
    private final List<BindableService> services;
    private Server server;

    public GrpcServerLifecycle(@Value("${grpc.server.port:9091}") int port, List<BindableService> services) {
        this.port = port;
        this.services = services;
    }

    @PostConstruct
    public void start() throws IOException {
        ServerBuilder<?> serverBuilder = ServerBuilder.forPort(port);
        for (BindableService service : services) {
            serverBuilder.addService(service);
        }

        server = serverBuilder.build().start();
        logger.info("gRPC server started on port {} with {} service(s)", port, services.size());
    }

    @PreDestroy
    public void stop() throws InterruptedException {
        if (server == null) {
            return;
        }
        logger.info("Stopping gRPC server");
        server.shutdown();
        server.awaitTermination();
    }
}
