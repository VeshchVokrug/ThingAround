package ru.veshvokrug.coownership.output.catalog;

import com.fasterxml.jackson.annotation.JsonValue;

/**
 * Действие над листингом совладения в catalog-service.
 * Зеркало C# enum {@code Core.Events.CoownershipListingAction}.
 * Сериализуется числом (0/1/2) — это единственный формат, который
 * System.Text.Json на стороне C# гарантированно десериализует
 * независимо от наличия {@code JsonStringEnumConverter}.
 *
 * @author Dmitrii Marchenko
 */
public enum CoownershipListingAction {
    CREATE(0),
    UPDATE(1),
    DELETE(2);

    private final int code;

    CoownershipListingAction(int code) {
        this.code = code;
    }

    @JsonValue
    public int getCode() {
        return code;
    }
}
