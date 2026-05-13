package ru.veshvokrug.recommendation.controller;

import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import ru.veshvokrug.recommendation.service.RecommendationService;

import java.util.List;

import static org.mockito.Mockito.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

/**
 * Тесты для {@link RecommendationsController}.
 */
class RecommendationsControllerTest {

    @Test
    void shouldReturnRecommendations() throws Exception {
        RecommendationService recommendationService = mock(RecommendationService.class);
        when(recommendationService.getRecommendations("u1", 2)).thenReturn(List.of("l1", "l2"));

        MockMvc mockMvc = MockMvcBuilders.standaloneSetup(new RecommendationsController(recommendationService)).build();

        mockMvc.perform(get("/api/v1/recommendations").param("userId", "u1").param("size", "2")
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isOk())
                .andExpect(content().json("{" +
                        "\"userId\":\"u1\"," +
                        "\"listings\":[\"l1\",\"l2\"]," +
                        "\"count\":2" +
                        "}"));
    }

    @Test
    void shouldReturnBadRequestWhenUserIdIsMissing() throws Exception {
        RecommendationService recommendationService = mock(RecommendationService.class);
        MockMvc mockMvc = MockMvcBuilders.standaloneSetup(new RecommendationsController(recommendationService)).build();

        mockMvc.perform(get("/api/v1/recommendations").param("size", "2")
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isBadRequest());
    }

    @Test
    void shouldReturnBadRequestWhenUserIdIsBlank() throws Exception {
        RecommendationService recommendationService = mock(RecommendationService.class);
        MockMvc mockMvc = MockMvcBuilders.standaloneSetup(new RecommendationsController(recommendationService)).build();

        mockMvc.perform(get("/api/v1/recommendations")
                        .param("userId", "   ")
                        .param("size", "2")
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isBadRequest());

        verifyNoInteractions(recommendationService);
    }
}

