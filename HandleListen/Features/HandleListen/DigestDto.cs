public record DigestPreferenceDto(DigestFrequency? Frequency);

public record SetDigestPreferenceRequest(DigestFrequency? Frequency);

public record SendDigestNowRequest(DigestFrequency Frequency);
