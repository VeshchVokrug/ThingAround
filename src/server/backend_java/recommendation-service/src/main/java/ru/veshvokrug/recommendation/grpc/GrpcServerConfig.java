package ru.veshvokrug.recommendation.grpc;

import io.grpc.Server;
import io.grpc.ServerBuilder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Конфигурация gRPC сервера для приема запросов от C# и других сервисов.
 *
 * @author Dmitrii Marchenko
 */
@Configuration
public class GrpcServerConfig {
    private static final Logger log = LoggerFactory.getLogger(GrpcServerConfig.class);

    @Value("${grpc.server.port:50054}")
    private int grpcServerPort;

    @Bean(destroyMethod = "shutdown")
    public Server grpcServer(RecommendationGrpcServiceImpl recommendationService) throws Exception {
        Server server = ServerBuilder
                .forPort(grpcServerPort)
                .addService(recommendationService)
                .build();

        server.start();
        log.info("gRPC server started on port {}", grpcServerPort);

        return server;
    }
}

