describe("Homepage", () => {
  beforeEach(() => {
    cy.visit("/");
  });
  it("Brings header", () => {
    cy.getBySel("main-heading").should("contain.text", "superplate");
  });

  it("Should have true href", () => {
    // https://github.com/cypress-io/cypress-example-recipes/blob/master/examples/testing-dom__tab-handling-links/cypress/integration/tab_handling_anchor_links_spec.js
    cy.getBySel("docs-btn-anchor")
      .should("have.prop", "href")
      .and("equal", "https://pankod.github.io/superplate/");
  });

  it("Should have icons", () => {
    cy.getBySel("icon").should("have.length", 6);
  });
});
