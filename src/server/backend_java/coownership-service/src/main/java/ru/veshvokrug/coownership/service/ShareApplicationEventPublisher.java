package ru.veshvokrug.coownership.service;

import ru.veshvokrug.coownership.model.entity.ShareApplication;

/**
 * Порт исходящих событий по заявкам на доли.
 */
public interface ShareApplicationEventPublisher {
    void publish(String eventType, ShareApplication shareApplication);
}
