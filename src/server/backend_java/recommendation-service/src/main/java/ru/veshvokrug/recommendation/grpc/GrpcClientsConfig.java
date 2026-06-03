package ru.veshvokrug.recommendation.grpc;

import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Bean;
import org.springframework.beans.factory.annotation.Value;

import io.grpc.ManagedChannel;
import io.grpc.ManagedChannelBuilder;

import catalog.protos.CatalogServiceGrpc;
import identityprofile.protos.IdentityProfileServiceGrpc;
import rental.protos.RentalServiceGrpc;

@Configuration
public class GrpcClientsConfig {

    @Value("${grpc.catalog.host:localhost}")
    private String catalogHost;
    @Value("${grpc.catalog.port:50051}")
    private int catalogPort;

    @Value("${grpc.identity.host:localhost}")
    private String identityHost;
    @Value("${grpc.identity.port:50052}")
    private int identityPort;

    @Value("${grpc.rental.host:localhost}")
    private String rentalHost;
    @Value("${grpc.rental.port:50053}")
    private int rentalPort;

    @Bean(destroyMethod = "shutdown")
    public ManagedChannel catalogChannel() {
        return ManagedChannelBuilder.forAddress(catalogHost, catalogPort).usePlaintext().build();
    }

    @Bean
    public CatalogServiceGrpc.CatalogServiceBlockingStub catalogBlockingStub(ManagedChannel catalogChannel) {
        return CatalogServiceGrpc.newBlockingStub(catalogChannel);
    }

    @Bean(destroyMethod = "shutdown")
    public ManagedChannel identityChannel() {
        return ManagedChannelBuilder.forAddress(identityHost, identityPort).usePlaintext().build();
    }

    @Bean
    public IdentityProfileServiceGrpc.IdentityProfileServiceBlockingStub identityBlockingStub(ManagedChannel identityChannel) {
        return IdentityProfileServiceGrpc.newBlockingStub(identityChannel);
    }

    @Bean(destroyMethod = "shutdown")
    public ManagedChannel rentalChannel() {
        return ManagedChannelBuilder.forAddress(rentalHost, rentalPort).usePlaintext().build();
    }

    @Bean
    public RentalServiceGrpc.RentalServiceBlockingStub rentalBlockingStub(ManagedChannel rentalChannel) {
        return RentalServiceGrpc.newBlockingStub(rentalChannel);
    }
}
