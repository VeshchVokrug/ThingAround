package ru.veshvokrug.coownership.service;

import ru.veshvokrug.coownership.model.entity.ShareApplication;

/**
 * Порт исходящих событий по заявкам на доли.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
public interface ShareApplicationEventPublisher {
    void publish(String eventType, ShareApplication shareApplication);
}
