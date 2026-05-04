package ru.veshvokrug.coownership.service;

import org.springframework.jdbc.core.ConnectionCallback;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Service;

import java.sql.PreparedStatement;

/**
 * Сервис транзакционных advisory-lock'ов для сериализации операций с одним бизнес-ключом.
 *
 * @author Dmitrii Marchenko 02.05.2026
 */
@Service
public class TransactionalLockService {
    private final JdbcTemplate jdbcTemplate;

    public TransactionalLockService(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    public void lock(String lockKey) {
        jdbcTemplate.execute((ConnectionCallback<Void>) connection -> {
            try (PreparedStatement statement = connection.prepareStatement(
                    "select pg_advisory_xact_lock(hashtextextended(?, 0))")) {
                statement.setString(1, lockKey);
                statement.execute();
            }
            return null;
        });
    }
}
