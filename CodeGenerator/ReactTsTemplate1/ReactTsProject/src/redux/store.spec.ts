import store from "./store";

describe("Redux Store", () => {
  it("should create store with counter reducer", () => {
    const currentState = store.getState();
    expect(currentState).toHaveProperty("counter");
  });
});
