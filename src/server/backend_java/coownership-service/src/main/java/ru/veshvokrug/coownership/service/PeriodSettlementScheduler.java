package ru.veshvokrug.coownership.service;

import net.javacrumbs.shedlock.spring.annotation.SchedulerLock;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

/**
 * Ночной планировщик закрытия завершенных периодов и запуска следующего периода.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Component
public class PeriodSettlementScheduler {
	private static final Logger log = LoggerFactory.getLogger(PeriodSettlementScheduler.class);

	private final PeriodLifecycleService periodLifecycleService;

	public PeriodSettlementScheduler(PeriodLifecycleService periodLifecycleService) {
		this.periodLifecycleService = periodLifecycleService;
	}

	@Scheduled(cron = "${coownership.period.settlement.cron:0 0 2 * * *}", zone = "UTC")
	@SchedulerLock(name = "periodSettlementJob", lockAtMostFor = "PT20M", lockAtLeastFor = "PT1M")
	public void settleClosedPeriods() {
		try {
			periodLifecycleService.settleFinishedPeriods();
		} catch (Exception ex) {
			log.error("Period settlement job failed", ex);
		}
	}
}
