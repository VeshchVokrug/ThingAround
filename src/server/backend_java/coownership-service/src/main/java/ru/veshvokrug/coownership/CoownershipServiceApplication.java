package ru.veshvokrug.coownership;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableScheduling;

@SpringBootApplication
@EnableScheduling
public class CoownershipServiceApplication {

    static void main(String[] args) {
        SpringApplication.run(CoownershipServiceApplication.class, args);
    }

}
