package ru.veshvokrug.coownership.service;

import net.javacrumbs.shedlock.spring.annotation.SchedulerLock;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

/**
 * Ночной планировщик отмены листингов, не собравших доли к дедлайну
 * финансирования. Без него листинги с истёкшим fundingDeadline
 * висели бы в статусе OPEN (и активной карточкой в каталоге) вечно.
 *
 * @author Dmitrii Marchenko
 */
@Component
public class FundingDeadlineScheduler {
	private static final Logger log = LoggerFactory.getLogger(FundingDeadlineScheduler.class);

	private final ListingService listingService;

	public FundingDeadlineScheduler(ListingService listingService) {
		this.listingService = listingService;
	}

	@Scheduled(cron = "${coownership.listing.funding-deadline-cron:0 30 2 * * *}", zone = "UTC")
	@SchedulerLock(name = "fundingDeadlineJob", lockAtMostFor = "PT20M", lockAtLeastFor = "PT1M")
	public void cancelExpiredListings() {
		try {
			int cancelled = listingService.cancelExpiredListings();
			if (cancelled > 0) {
				log.info("Отменено листингов с истёкшим дедлайном финансирования: {}", cancelled);
			}
		} catch (Exception ex) {
			log.error("Funding deadline job failed", ex);
		}
	}
}
