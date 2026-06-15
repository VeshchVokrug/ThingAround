package ru.veshvokrug.coownership.service;

import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.output.catalog.CoownershipListingAction;

/**
 * Порт синхронизации листингов совладения с catalog-service.
 * Вызывается внутри транзакции бизнес-операции; реализация обязана
 * гарантировать доставку (outbox), а не отправлять сообщение напрямую.
 *
 * @author Dmitrii Marchenko
 */
public interface CatalogListingSyncPublisher {

    /**
     * Запланировать отправку текущего состояния листинга в каталог.
     *
     * @param action  что произошло с листингом (Create/Update/Delete)
     * @param listing листинг в актуальном состоянии
     */
    void publish(CoownershipListingAction action, CoownershipListing listing);
}
