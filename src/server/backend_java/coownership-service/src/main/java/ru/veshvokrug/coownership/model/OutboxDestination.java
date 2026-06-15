package ru.veshvokrug.coownership.model;

/**
 * Назначение outbox-сообщения. Все сообщения уходят в RabbitMQ,
 * но в разные exchange и в разных форматах:
 * внутренние события — в topic exchange coownership-events (plain JSON),
 * команды синхронизации листингов — в exchange каталога (MassTransit-конверт).
 *
 * @author Dmitrii Marchenko
 */
public enum OutboxDestination {
    COOWNERSHIP_EVENTS,
    CATALOG_RABBITMQ
}
