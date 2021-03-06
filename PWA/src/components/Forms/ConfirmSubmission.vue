<template>
  <v-container class="text-center">
    <!-- Submit button at the bottom of a form, next to the 'validate' button -->
    <v-btn color="success" dark @click.stop="signalParentValidate">
      {{ $t("components_Forms_ConfirmSubmission_submit") }}
    </v-btn>
    <!-- END Submit button -->
    
    <!-- Dialog box that appears when client presses 'submit' -->
    <v-dialog v-model="displaySubmit" max-width="75%">
      <!-- No errors on the form -->
      <template v-if="isValid">
        <v-card>
          <div v-if="!loading">
            <v-card-title class="headline" id="confirm">
              {{ $t("components_Forms_ConfirmSubmission_confirm") }}
            </v-card-title>

            <v-card-text>
              {{ $t("components_Forms_ConfirmSubmission_confirm_desc") }}
            </v-card-text>

            <v-card-text v-if="this.totalEdited > 0">
              <em>
                {{ $t("components_Forms_ConfirmSubmission_edited") }}
              </em>

              <!-- 
                If there were any changes to the form, disable 
                submission until both provider and employer re-sign. 
              -->
              <v-container fluid>
                <v-checkbox
                  color="success"
                  v-model="reSigned"
                  :label="$t('components_Forms_ConfirmSubmission_employer')"
                  value="Employer"
                ></v-checkbox>
                <v-checkbox
                  color="success"
                  v-model="reSigned"
                  :label="$t('components_Forms_ConfirmSubmission_provider')"
                  value="Provider"
                ></v-checkbox>
              </v-container>

              <v-card
                color="red lighten-2"
                v-if="!reSigned.includes('Employer')"
              >
                {{ $t("components_Forms_ConfirmSubmission_employer_desc") }}
              </v-card>
              <v-card
                color="red lighten-2"
                v-if="!reSigned.includes('Provider')"
              >
                {{ $t("components_Forms_ConfirmSubmission_provider_desc") }}
              </v-card>
            </v-card-text>
            <!-- END require re-sign -->

            <v-card-actions>
              <v-spacer></v-spacer>

              <!-- Confirm if user is ready to submit -->
              <v-btn
                class="white--text"
                color="red"
                @click="displaySubmit = false"
              >
                {{ $t("components_Forms_ConfirmSubmission_cancel") }}
              </v-btn>
              
              <!-- Disable submission if offline -->
              <template v-if="onlineStatus">
                <v-btn
                  class="white--text"
                  color="green darken-1"
                  :disabled="canSubmit"
                  @click="submit"
                >
                  {{ $t("components_Forms_ConfirmSubmission_submit") }}
                </v-btn>
              </template>
              <template v-else>
                {{ $t("components_Forms_ConfirmSubmission_offline") }}
              </template>
            </v-card-actions>
          </div>

          <div v-else>
            <div v-if="!returnHome">
              <!-- Submitting the form -->
              <v-dialog value="true" hide-overlay persistent width="300">
                <v-card color="primary" dark>
                  <v-card-text class="text-center">
                    <v-progress-circular
                      indeterminate
                      color="white"
                      id="progress"
                      class="mb-0"
                      :size="50"
                    ></v-progress-circular>
                  </v-card-text>
                  <v-card-text class="text-center">
                    {{ $t("components_Forms_ConfirmSubmission_submitting") }}
                  </v-card-text>
                </v-card>
              </v-dialog>
              <!-- END Submitting the form -->
            </div>

            <!-- Display submission status -->
            <div v-else>
              <!-- Successfully submitted form -->
              <div v-if="submissionStatus">
                <v-dialog value="true" hide-overlay persistent width="300">
                  <v-card>
                    <v-card-title
                      class="headline text-center success white--text"
                      id="submited"
                    >
                      {{ $t("components_Forms_ConfirmSubmission_submitted") }}
                    </v-card-title>
                    <v-card-text>
                      <v-card-text class="text-center" id="submission-complete">
                        {{
                          $t(
                            "components_Forms_ConfirmSubmission_submitted_desc"
                          )
                        }}
                      </v-card-text>
                      <v-card-text class="text-center">
                        <v-btn
                          class="mt-3 center"
                          color="indigo"
                          @click="resetForm()"
                          dark
                        >
                          {{ $t("components_Forms_ConfirmSubmission_home") }}
                        </v-btn>
                      </v-card-text>
                    </v-card-text>
                  </v-card>
                </v-dialog>
              </div>
              <!-- END Successfully submitted form -->

              <!-- Error submitting the form -->
              <div v-else>
                <v-alert class="headline" id="failure" type="error">
                  {{ $t("components_Forms_ConfirmSubmission_error") }}
                </v-alert>

                <v-card-text>
                  {{ $t("components_Forms_ConfirmSubmission_error_desc") }}
                </v-card-text>
              </div>
              <!-- END Error submitting the form -->

            </div>
            <!-- END Display submission status -->

          </div>
        </v-card>
      </template>
      <!-- END No errors on the form -->

      <!-- Errors on the form-->
      <template v-else>
        <v-card>
          <v-card-title class="headline text-danger" id="invalid">
            {{ $t("components_Forms_ConfirmSubmission_invalid") }}
          </v-card-title>

          <v-card-text>
            <div
              v-html="$t('components_Forms_ConfirmSubmission_invalid_desc')"
            ></div>
            <v-card
              color="red lighten-2"
              v-for="(error, index) in errors"
              :key="index"
            >
              <strong>{{ error }}</strong>
            </v-card>
          </v-card-text>
        </v-card>
      </template>
      <!-- END Errors on the form-->
    </v-dialog>
    <!-- END Dialog box ... when presses 'submit' -->

  </v-container>
</template>

<style>
  .v-card__text {
    word-break: normal; /* maybe !important  */
  }
  .v-card__title {
    word-break: normal; /* maybe !important  */
    justify-content: center;
  }
  .v-progress-circular {
    margin: 1rem;
  }
  .p {
    margin-bottom: 0 !important;
  }
  .v-application p {
    margin-bottom: 0 !important;
  }
</style>

<script>
  import axios from "axios";
  import store from "@/store/index.js";
  import { mapFields } from "vuex-map-fields";
  import { mapMutations } from "vuex";
  import { FORM, FORM_TYPE } from "@/components/Utility/Enums.js";

  export default {
    name: "ConfirmSubmission",
    props: {
      // The cols in the datatable
      cols: {
        type: Array,
        default: null,
      },

      //If the information is valid.
      valid: {
        type: Boolean,
        default: false,
      },

      // Signal that parent form has completed validation
      validationSignal: {
        type: Boolean,
        default: false,
      },

      // The list of errors from the parent's validation function
      errors: {
        type: Array,
        default: null,
      },

      // The amount of errors from the parent's validation function
      numErrors: {
        type: Number,
        default: 0,
      },
    },

    data() {
      return {
        // Hide or display the submit pop-up
        displaySubmit: false,

        //Log of POST connection.
        loading: false,

        //Track when the POST completes
        submissionStatus: false,

        //Flag for once POST has been successful/failed
        returnHome: false,

        //Data to be submitted
        submitData: null,

        // All the errors of this form
        isValid: this.valid,
        waitingOnParent: false,

        // Provider and employer re-signed the form
        reSigned: [],
      };
    },

    computed: {
      url: function () {
        //URL for the AppServer
        if (FORM[this.formChoice] === FORM.OR004_MILEAGE)
          return process.env.VUE_APP_SERVER_URL.concat(
            "Timesheet/SubmitMileage"
          );
        else return process.env.VUE_APP_SERVER_URL.concat("Timesheet/Submit");
      },
      canSubmit: function () {
        return (
          this.totalEdited > 0 && !(this.reSigned.length === 2) && this.isValid
        );
      },
      ...mapFields(["formChoice", "formId", "guid", "onlineStatus"]),
      formType: function () {
        return FORM_TYPE[this.formChoice];
      },
      totalEdited: function () {
        if (this.formType !== undefined && store.getters !== undefined)
          return store.getters[this.formType + "/getField"]("totalEdited");
        else return 0;
      },
      formFields: function () {
        if (this.formType !== undefined && store.getters !== undefined)
          return store.getters[this.formType + "/getField"]("formFields");
        else return null;
      },
    },

    watch: {
      // The parent form has finished validating all fields on the
      // IDD Timesheet. Display errors or submit
      validationSignal() {
        var numErrors = this.errors.length;
        if (numErrors > 0) {
          this.isValid = false;
        } else {
          this.isValid = true;
        }

        if (this.waitingOnParent === true) {
          this.waitingOnParent = false;
          this.displaySubmit = true;
        }
      },
    },

    methods: {
      ...mapMutations({
        resetState: "resetState",
        resetServiceDelivered: "ServiceDelivered/resetState",
        resetMileage: "Mileage/resetState",
      }),

      // Reset all submission values
      resetValid() {
        this.reSigned = [];
        this.loading = false;
        this.submissionStatus = false;
        this.returnHome = false;
      },

      //Formats the data to be posted
      formatData() {
        var submitData = {};
        Object.entries(this.formFields).forEach(([key, value]) => {
          submitData[key] = {};
          submitData[key]["value"] = value["value"];
          submitData[key]["wasEdited"] = !value["disabled"];
        });
        var sheetType = "";
        if ("timesheet" in this.formFields) {
          sheetType = "timesheet";
        } else if ("mileagesheet" in this.formFields) {
          sheetType = "mileagesheet";
        }
        if (sheetType.localeCompare("") !== true) {
          submitData[sheetType]["value"] = [];
          Object.entries(this.formFields[sheetType]["value"]).forEach(
            ([key, value]) => {
              key;
              var row = {};

              this.cols.forEach((col) => {
                row[col] = value[col];
              });
              row["wasEdited"] = !value["disabled"];

              submitData[sheetType]["value"].push(row);
            }
          );
        }

        return submitData;
      },

      // Send signal to parent component to validate
      signalParentValidate() {
        // Reset the submission values
        this.resetValid();

        // Set flag to wait on parent
        this.waitingOnParent = true;

        // Send signal to parent component to validate input fields
        this.$emit("click");
      },

      //Submits form to AppServer.
      submit() {
        // Do not post timesheet if any:
        //   - The form is invalid
        //   - There were edits, but employer and provider didn't re-sign
        if (this.canSubmit) {
          return false;
        }

        // Else, post timesheet
        this.loading = true;
        let self = this;

        // Finally, prepare the form data and send to the backend
        this.submitData = this.formatData();
        if (this.errors.length === 0) {
          this.submitData["id"] = this.formId;
          this.submitData["guid"] = this.guid;
          this.submitData["formChoice"] = FORM[this.formChoice];
          console.log("submitData form ConfirmSubmission:410", this.submitData);
          axios
            .post(this.url, this.submitData, {
              headers: {
                "content-type": "application/json",
              },
            })
            .then(function (response) {
              if (response["data"]["response"] == "ok") {
                console.log("Finished posting!");
                self.submissionStatus = true;

                //Return to home here?
                self.returnHome = true;
              }
            })
            .catch(function (error) {
              console.log(error);
            });
        }
      },

      resetForm() {
        // Reset the vuex store
        this.resetState();
        this.resetServiceDelivered();
        this.resetMileage();
        window.location.href = "/";
      },
    },
  };
</script>
